## MODIFIED Requirements

### Requirement: Proyecto Avalonia en la solución
El repositorio SHALL contener un proyecto `src/app/GitExtensions.Avalonia` que compile como
`net10.0` con Avalonia 11.3, use el tema Fluent, inicialice
`ThreadHelper.JoinableTaskContext` con `AvaloniaSynchronizationContext` en
`OnFrameworkInitializationCompleted()`, desactive las decoraciones nativas de la ventana y
proporcione la ventana principal de la shell IDE-like y el flujo de apertura de repositorio.

#### Scenario: El proyecto compila en Windows
- **WHEN** se ejecuta `dotnet build` sobre `GitExtensions.Avalonia.csproj` en Windows
- **THEN** la compilación termina sin errores

#### Scenario: El proyecto compila en Linux
- **WHEN** se ejecuta `dotnet build` sobre `GitExtensions.Avalonia.csproj` en Linux
- **THEN** la compilación termina sin errores

#### Scenario: El proyecto está en la solución
- **WHEN** se abre `GitExtensions.slnx`
- **THEN** `src/app/GitExtensions.Avalonia/GitExtensions.Avalonia.csproj` aparece listado

#### Scenario: La shell principal reemplaza la ventana vacía
- **WHEN** se inicia la aplicación Avalonia
- **THEN** se muestra la composición principal con barra superior, rails laterales, área central y barra inferior
- **AND** el área central puede alojar la vista de bienvenida, la vista del repositorio o la lista de commits
- **AND** la barra superior propia es el único chrome de ventana visible
