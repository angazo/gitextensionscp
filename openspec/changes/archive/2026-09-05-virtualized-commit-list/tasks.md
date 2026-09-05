## 1. Adaptador de streaming y modelo de presentación

- [x] 1.1 Añadir `CommitPresentation` (subject, autor, fecha, object id corto) como proyección inmutable de `GitRevision`; verificar con tests unitarios que cubran un commit normal y uno con mensaje multilínea (solo subject, sin body).
- [x] 1.2 Añadir `CommitListState` (Loading/Empty/Loaded/Error) siguiendo el patrón de estados de presentación de la apertura de repositorios; verificar sus transiciones válidas con tests unitarios.
- [x] 1.3 Añadir el contrato interno `IRevisionLogPort` y un adaptador de producción que envuelve `RevisionReader.GetLog` en un hilo de fondo, traduciendo sus lotes `IObserver` a la forma del puerto; verificar con un test que los tests propios del puerto no requieren un `IGitModule`/proceso falso (el adaptador se cubre por separado en 1.5).
- [x] 1.4 Añadir `CommitLogService`, dueño de un único `CancellationTokenSource` por stream activo, que aplica actualizaciones por lotes a una colección enlazada al hilo de UI y mapea cada lote a `CommitPresentation`; verificar el loteo y el orden con un `IRevisionLogPort` falso que emite lotes controlados.
- [x] 1.5 Verificar el comportamiento de cancelación: iniciar un stream nuevo cancela y libera el anterior, y un stream cancelado no se manifiesta como estado `Error`; cubrir con tests unitarios usando el puerto falso.

## 2. Vista de lista de commits e integración en la shell

- [x] 2.1 Añadir el ViewModel de la lista de commits, exponiendo la colección observable, `CommitListState` y un comando de reintento; verificar las transiciones de estado (loading → loaded, loading → empty, loading → error → retry) con tests unitarios.
- [x] 2.2 Añadir la vista de lista de commits usando un control de lista virtualizado con altura de fila fija y un `DataTemplate` limitado a subject/autor/fecha/id corto; verificar que no se renderiza grafo, carriles ni decoraciones de refs/labels.
- [x] 2.3 Conectar la lista de commits al área central de la shell para que reemplace el contenido de repositorio una vez que un repositorio se abre con éxito; verificar el escenario de `avalonia-shell` sobre el área central alojando la lista de commits.
- [x] 2.4 Conectar el cambio de repositorio con `CommitLogService`: abrir un repositorio distinto cancela el stream anterior e inicia uno nuevo; verificar con un test de integración usando el puerto falso y dos aperturas de repositorio simuladas.
- [x] 2.5 Registrar el nuevo servicio y ViewModel en la composición DI de Avalonia sin referenciar `GitUI` ni `GitExtUtils.WinForms`; verificar el grafo de dependencias y la compilación `net10.0`.

## 3. Estados de presentación en la vista

- [x] 3.1 Mostrar el estado de carga mientras no ha llegado el primer lote; verificar visualmente y con un test a nivel de ViewModel.
- [x] 3.2 Mostrar el estado de historial vacío para un repositorio sin commits, distinto del estado de error; verificar con un puerto falso que completa con cero lotes.
- [x] 3.3 Mostrar el estado de error con una acción de reintento cuando el stream falla; verificar que reintentar vuelve a invocar el puerto y puede recuperar el estado `Loaded`.

## 4. Validación con repositorios grandes

- [x] 4.1 Validar manualmente el scroll y el tiempo de carga inicial frente a un clon local con 100.000+ commits en al menos una plataforma; registrar el comportamiento observado (virtualización fluida o problemas encontrados) en la nota de finalización de esta tarea.
- [x] 4.2 Si la validación manual revela un problema de virtualización o threading, dejarlo registrado como nota de seguimiento en este change antes de archivarlo, según la guía de rollback del diseño.

Nota de validación manual: se probaron repositorios de 100.000 y 160.000 commits. El scroll y la carga inicial funcionaron correctamente; también se abrieron varios repositorios consecutivamente y cada lista se cargó correctamente, sin problemas observados de virtualización ni threading.

## 5. Verificación y compatibilidad

- [x] 5.1 Ejecutar los nuevos tests headless/unitarios del adaptador, el servicio y los ViewModels en `net10.0`; verificar que cubren loteo, cancelación, orden y estados de presentación sin un proceso git real.
- [x] 5.2 Ejecutar `dotnet build GitExtensions.slnx`; verificar que la solución primaria y la shell Avalonia compilan sin referencias WinForms nuevas.
- [ ] 5.3 Ejecutar la verificación Linux existente; verificar que los nuevos tests no requieren display nativo ni WinForms.
- [x] 5.4 Compilar la solución WinForms de referencia y ejecutar su verificación aplicable; verificar que el uso de `RevisionReader` por parte de `RevisionGridControl` no se ve afectado.
- [x] 5.5 Ejecutar `openspec validate --changes "virtualized-commit-list" --strict`; verificar que todos los artefactos y escenarios cumplen el schema antes de solicitar revisión.
