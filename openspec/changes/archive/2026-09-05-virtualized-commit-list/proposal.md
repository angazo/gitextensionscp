## Why

La shell Avalonia ya puede abrir un repositorio y mostrar su contexto básico de Git (#23), pero el área central todavía no ofrece ninguna vista del historial del repositorio. `RevisionReader` ya emite commits para `RevisionGridControl` en WinForms mediante un pipeline por lotes con `IObserver<IReadOnlyList<GitRevision>>`, pero nada ha demostrado todavía que ese pipeline pueda alimentar una lista virtualizada en Avalonia sin bloquear el hilo de UI ni materializar cada fila, especialmente en repositorios con 100K+ commits.

Este change entrega el slice 1.3: una lista de commits plana, de solo lectura y virtualizada en la shell Avalonia, alimentada por `RevisionReader`. Cierra el último riesgo de infraestructura abierto de la Fase 1 (§10.3/§10.7 del análisis de migración) antes de que la Fase 2 añada el grafo de commits y el diff viewer. El issue de seguimiento es [#27](https://github.com/madialeva/gitextensionscp/issues/27), asociado al milestone `Fase 1 — Walking skeleton Avalonia`.

## What Changes

- Añadir una vista de lista de commits que reemplaza el contenido de repositorio del área central de la shell una vez que un repositorio está abierto.
- Adaptar el pipeline por lotes de `RevisionReader.GetLog` (basado en `IObserver`) a una colección virtualizada apta para Avalonia, trasladando las actualizaciones al hilo de UI sin bloquearlo.
- Mostrar de cada commit su subject, autor, fecha y object id corto; sin grafo, carriles ni decoraciones de refs/labels (solo lista plana).
- Cancelar el stream de log en curso y el proceso git subyacente cuando cambia el repositorio activo o se cierra la vista, e iniciar un stream nuevo para el repositorio recién abierto.
- Representar los estados de carga, vacío (sin commits) y error de la lista de commits, siguiendo el mismo patrón de estados de presentación usado en la apertura de repositorios.
- Añadir un doble de prueba headless para el pipeline de streaming del log (sin proceso git real) que cubra de forma determinista el loteo, la cancelación y el orden.
- Validar el comportamiento de scroll/virtualización frente a un repositorio grande (100K+ commits) para cerrar el riesgo de pipeline señalado en el análisis de migración; esta validación es manual/exploratoria y no se entrega como test de rendimiento automatizado.
- Mantener la implementación en `net10.0`, multiplataforma y sin dependencias de `GitUI`/`GitExtUtils.WinForms`.

## Capabilities

### New Capabilities

- `avalonia-commit-list`: Lista de commits plana, virtualizada y de solo lectura alimentada por `RevisionReader`, incluyendo el comportamiento de streaming/cancelación y los estados de presentación de carga/vacío/error.

### Modified Capabilities

- `avalonia-shell`: El área central del workspace gana una tercera vista alojada (la lista de commits) junto a las vistas de bienvenida e información de repositorio ya establecidas.

## Impact

- `src/app/GitExtensions.Avalonia`: nueva vista de lista de commits, ViewModel y un servicio adaptador de streaming que conecta `RevisionReader.GetLog` con una colección observable consumida por la UI.
- `tests/app/UnitTests/GitExtensions.Avalonia.Tests`: nuevos tests headless para el adaptador de streaming (loteo, cancelación, orden) usando un doble de prueba en lugar de un proceso git real.
- Reutiliza `GitCommands.RevisionReader`, `GitRevision`, `IGitModule` y `ObjectId` del core existente; no modifica sus contratos públicos.
- `openspec/changes/virtualized-commit-list`: specs, diseño y tareas del #27.
- No toca `GitUIPluginInterfaces` ni ninguna API de cara a plugins.
