## Purpose

Proporciona una vista plana, virtualizada y de solo lectura del historial de commits de un repositorio en la shell Avalonia, alimentada por el pipeline de log existente de `RevisionReader`, sin grafo ni operaciones de escritura.

## ADDED Requirements

### Requirement: Lista de commits del repositorio abierto
La shell SHALL mostrar una lista virtualizada de commits del repositorio actualmente abierto, poblada mediante el streaming por lotes del log del repositorio sin bloquear el hilo de UI.

#### Scenario: Los commits se cargan tras abrir un repositorio
- **WHEN** un repositorio se abre correctamente
- **THEN** la shell empieza a cargar su log de commits
- **AND** los commits se vuelven visibles progresivamente conforme llegan los lotes, sin congelar la UI

#### Scenario: Scroll en un historial grande
- **WHEN** el repositorio abierto tiene 100.000 o más commits
- **THEN** el usuario puede desplazarse por la lista de commits con fluidez
- **AND** la lista no materializa una fila visual por cada commit a la vez

### Requirement: Contenido de la lista de commits
Cada fila de commit visible SHALL mostrar su subject, autor, fecha y un object id corto. La lista SHALL NOT renderizar un grafo de commits, carriles ni decoraciones de refs/labels.

#### Scenario: Contenido de una fila de commit
- **WHEN** se renderiza una fila de commit
- **THEN** la fila muestra el subject, el autor, la fecha y el object id corto del commit
- **AND** la fila no muestra carriles de grafo ni decoraciones de refs/labels

### Requirement: Cancelación al cambiar de repositorio
La shell SHALL cancelar un stream de log de commits en curso, incluyendo su proceso git subyacente, cuando cambia el repositorio activo o se cierra la vista de lista de commits, y SHALL NOT mostrar commits de un stream que ya no corresponda al repositorio activo.

#### Scenario: Cambiar a otro repositorio
- **WHEN** el usuario abre un repositorio distinto mientras un stream de log de commits está en curso
- **THEN** el stream anterior se cancela
- **AND** la lista de commits muestra solo los commits del repositorio recién abierto

#### Scenario: Cerrar la vista de lista de commits
- **WHEN** el usuario navega fuera de la lista de commits antes de que su stream complete
- **THEN** el stream en curso se cancela sin producir un error

### Requirement: Estados de presentación de la lista de commits
La lista de commits SHALL representar de forma distinta los estados de carga, historial vacío y error, y un error SHALL NOT bloquear la shell ni dejar visible una lista obsoleta.

#### Scenario: Estado de carga
- **WHEN** el stream de log de commits ha empezado pero todavía no ha llegado ningún lote
- **THEN** la lista de commits muestra un estado de carga

#### Scenario: Repositorio vacío
- **WHEN** el repositorio abierto no tiene commits
- **THEN** la lista de commits muestra un estado de historial vacío en lugar de un error

#### Scenario: Fallo de lectura
- **WHEN** la lectura del log de commits falla
- **THEN** la lista de commits muestra un estado de error con una forma de reintentar
- **AND** la shell permanece operativa
