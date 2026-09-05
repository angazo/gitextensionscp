## Context

Ver `proposal.md` para la motivación. La shell ya abre un repositorio y muestra una vista `avalonia-repository-opening` construida alrededor de `RepositoryOpeningService` y pequeños puertos internos (`IRepositoryHistoryPort`, `IRepositoryFolderPicker`, `IRepositoryReader`) que mantienen el I/O de Git fuera del ViewModel y detrás de dobles de prueba.

El pipeline de log de commits del core es `GitCommands.RevisionReader.GetLog(IObserver<IReadOnlyList<GitRevision>> subject, ...)`. Ejecuta un proceso `git log` desacoplado, parsea la salida en el hilo que lo invoca y empuja lotes a `subject.OnNext` (un primer lote pequeño de 100 para feedback rápido, y luego lotes de hasta 25.000 o cada 500ms) antes de llamar a `subject.OnCompleted()`. `RevisionGridControl` en WinForms lo invoca vía `ThreadHelper.FileAndForget` en un hilo de fondo y traslada las actualizaciones de vuelta al grid. `GetLog` es un método bloqueante y síncrono mientras dura la lectura; la única forma de detenerlo antes de tiempo es el `CancellationToken`, que se comprueba entre chunks y lanza `OperationCanceledException` desde el trabajo en segundo plano.

## Goals / Non-Goals

**Goals:**

- Conectar el loteo por push de `RevisionReader.GetLog` en un hilo de fondo con una colección enlazable de Avalonia, sin bloquear el hilo de UI ni copiar todo el resultado antes de mostrar nada.
- Hacer el stream de log cancelable por cada apertura de repositorio y por el ciclo de vida de la vista, garantizando un único stream activo a la vez.
- Mantener el adaptador testeable mediante un puerto interno, siguiendo el patrón de `avalonia-repository-opening`, para que los tests no lancen un proceso `git log` real.
- Demostrar que la virtualización se sostiene en historiales muy grandes mediante validación manual/exploratoria, sin añadir un test de rendimiento automatizado a la suite (inestable y dependiente del entorno).

**Non-Goals:**

- No hay grafo de commits, carriles ni refs/labels (Fase 2.1).
- No hay selección, diff ni vista de detalle impulsada por la lista (Fase 2.2+).
- No hay UI de filtrado/selector de rama más allá de loguear la rama actualmente abierta del repositorio.
- No se modifica el contrato público ni la lógica de parseo de `RevisionReader`.

## Decisions

### 1. Un `CommitLogService` detrás de un `IRevisionLogPort`, siguiendo el patrón de apertura de repositorios

Un nuevo `IRevisionLogPort` interno expondrá un método que devuelve un stream por lotes (con forma similar a `IAsyncEnumerable<IReadOnlyList<GitRevision>>` o un esquema equivalente de subscribe/callback — se cerrará durante la implementación) para una ruta de repositorio y un cancellation token dados. El adaptador de producción envuelve `RevisionReader.GetLog`, traduciendo su callback `IObserver` a la forma del puerto y ejecutándolo en un hilo de fondo vía `Task.Run`. Un `CommitLogService` consume el puerto, es dueño del ciclo de vida del `CancellationTokenSource` y expone una superficie de actualización amigable con `ObservableCollection<CommitPresentation>` para el ViewModel.

Esto mantiene `RevisionReader` intacto y permite a los tests sustituir un puerto falso que emite lotes preparados de forma síncrona o con retardos controlados, sin un proceso git real — el mismo esquema ya probado para `IRepositoryReader` en 1.2.

**Alternativa considerada:** invocar `RevisionReader.GetLog` directamente desde el ViewModel con una implementación `IObserver` inline. Se descarta: lanzaría un proceso real en cada test que ejercite el ViewModel y acoplaría el código de presentación directamente a los internos de `GitCommands`.

### 2. `CommitPresentation` como proyección inmutable

Cada lote se transforma en un registro `CommitPresentation` (Subject, Author, Date, ShortId) antes de llegar a la UI, siguiendo el mismo patrón que `RepositoryPresentation`. El mapeo ocurre fuera del hilo de UI; solo los registros resultantes cruzan hacia la colección observable.

**Alternativa considerada:** enlazar directamente a `GitRevision`. Se descarta: `GitRevision` es un tipo mutable del core no diseñado como modelo de presentación de UI, y enlazarlo filtraría tipos de `GitCommands` en el XAML y acoplaría la vista a campos (parent ids, notes, cuerpo en bruto) que este slice no usa.

### 3. Actualizaciones de la colección por lotes en vez de fila a fila

El adaptador traslada cada lote de `RevisionReader.GetLog` como una única actualización masiva a la colección del hilo de UI (por ejemplo mediante inserción tipo `AddRange` o un reemplazo de `ObservableCollection` por lotes), en lugar de invocar `Add` por cada commit. Esto se corresponde con el propio loteo del reader (100 commits para el primer lote, luego hasta 25.000 o cada 500ms) y evita un dispatch al hilo de UI y una notificación de cambio de colección por cada commit, lo que anularía la virtualización en historiales grandes.

**Alternativa considerada:** despachar una actualización de UI por cada commit parseado. Se descarta: reintroduce exactamente el sobrecoste por fila que el propio loteo del reader ya evita en el lado de la lectura, con riesgo de saturar el hilo de UI en repositorios grandes.

### 4. Control de lista virtualizado con filas de altura fija

La lista de commits usará un control de lista virtualizado de Avalonia (un `ItemsControl`/`ListBox` virtualizante, o `TreeDataGrid` en modo plano) con altura de fila fija y un `DataTemplate` limitado a subject/autor/fecha/id corto. La altura fija evita pasadas de medición por elemento, que es el principal enemigo del rendimiento de la virtualización en la práctica.

**Alternativa considerada:** un `ItemsControl` no virtualizante (host `StackPanel`). Se descarta de plano: materializaría el visual de cada fila, exactamente el riesgo que este change existe para cerrar.

### 5. Cancelación al cambiar de repositorio y al cerrar la vista

`CommitLogService` mantiene un único `CancellationTokenSource` para el stream activo. Abrir un nuevo repositorio, o que la shell navegue fuera de la vista de repositorio, cancela y libera la fuente actual antes de iniciar una nueva (si aplica). La tarea de fondo del adaptador trata `OperationCanceledException` como una finalización esperada y silenciosa, no como un error.

**Alternativa considerada:** dejar que un stream obsoleto siga corriendo e ignorar simplemente sus resultados. Se descarta: filtraría un proceso `git log` y un hilo de fondo por cada cambio de repositorio, acumulándose a lo largo de una sesión.

### 6. Errores e historial vacío como estados de presentación

Un `CommitListState` (Loading / Empty / Loaded / Error) determina qué muestra la vista, siguiendo la misma forma usada para la apertura de repositorios. Un repositorio vacío (sin commits) es un estado válido, no un error. Los fallos que emerjan del reader o del proceso git se convierten en `Error` con una acción de reintento.

## Risks / Trade-offs

- **[`GetLog` síncrono y bloqueante]** El único punto de cancelación cooperativa está entre chunks → ejecutarlo siempre vía `Task.Run`/hilo de fondo y nunca esperarlo inline en el hilo de UI.
- **[Primer lote grande en historiales enormes]** Incluso el lote de "feedback rápido" de 100 podría tardar en aparecer si un repositorio tiene filtros costosos → este slice loguea la rama actualmente abierta sin filtros adicionales, manteniendo `git log` tan barato como la referencia de WinForms.
- **[Validación de rendimiento manual]** Ningún test automatizado afirma el escenario de 100K+ commits, ya que depende de un repositorio local grande y de scroll real → tratarlo como paso de verificación manual en `tasks.md` y registrar el resultado, no como puerta de CI.
- **[Saturación de la colección]** Las actualizaciones masivas a una colección enlazada pueden seguir causando parones de UI si se implementan de forma ingenua → preferir reemplazar/extender la lista subyacente en una sola operación por lote y dejar que el panel virtualizante remida solo las filas visibles.
- **[Fugas de proceso al cambiar de repositorio rápidamente]** Aperturas sucesivas rápidas podrían solapar cancelación y liberación → serializar "cancelar el anterior, luego iniciar el nuevo" dentro de `CommitLogService` en vez de disparar ambos en paralelo.

## Migration Plan

1. Añadir `CommitPresentation`, `CommitListState` y el contrato `IRevisionLogPort` junto con un adaptador de producción que envuelve `RevisionReader.GetLog`.
2. Añadir `CommitLogService` con propiedad de la cancelación y actualizaciones por lotes, más un puerto falso testeable en headless.
3. Añadir el ViewModel y la vista de la lista de commits, conectados al área central de la shell tras abrir un repositorio con éxito.
4. Añadir tests headless para el adaptador y el servicio (loteo, cancelación, orden) usando el puerto falso; sin proceso git real en los tests.
5. Validar manualmente el scroll y el tiempo de carga frente a un clon local con 100K+ commits; registrar el resultado en `tasks.md`.
6. Si aparecen problemas de virtualización o de threading, el fallback es mantener la vista actual de solo información de repositorio y dejar la nueva lista de commits pendiente de más investigación; no hay cambios en el core ni en la API de plugins, así que el rollback se limita al proyecto Avalonia.

## Open Questions

- El control de lista concreto de Avalonia (`ItemsControl`/`ListBox` virtualizante frente a `TreeDataGrid` en modo plano) se decidirá durante la implementación según cuál ofrezca virtualización de altura fija más simple en 11.3; no cambia los contratos de los puertos, el modelo de presentación ni el desglose de tareas.
