# avalonia-shell Specification

## Purpose
Shell Avalonia del proyecto: proyecto `GitExtensions.Avalonia` con tema Fluent que compila
en Windows y Linux. Es el punto de partida para todos los cambios de UI Avalonia en Fase 1
y siguientes. Establecida por el change 1.1a (`hello-avalonia`); ampliada con inicializacion
de JTF en el change 1.1b (`jtf-replumbing`) y con DI + delegates en el change 1.1c
(`di-shell-delegates`).

## Requirements
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

### Requirement: Tema Fluent con claro/oscuro
La aplicación SHALL usar `FluentTheme` como tema predeterminado, con soporte para variantes
clara y oscura vía `ThemeVariant`.

#### Scenario: Tema oscuro por defecto
- **WHEN** la aplicación se inicia
- **THEN** el tema aplicado es la variante oscura de Fluent

#### Scenario: Ventana con título correcto
- **WHEN** la aplicación se inicia
- **THEN** la ventana principal muestra el título "GitExtensions"

### Requirement: Dependencias declaradas
El proyecto SHALL referenciar los paquetes `Avalonia`, `Avalonia.Desktop`,
`Avalonia.Themes.Fluent` (version 11.3.x) y `CommunityToolkit.Mvvm`, con las versiones
centralizadas en `Directory.Packages.props`. `Microsoft.VisualStudio.Threading` se obtiene
via la referencia global en `Directory.Build.targets`. El proyecto SHALL tener un
`ProjectReference` a `GitExtUtils` (cross-platform, sin WinForms) para acceder a
`ThreadHelper`, y un `ProjectReference` a `GitCommands` (cross-platform, `net10.0`) para
registrar sus servicios en el contenedor DI y acceder a tipos como `IGitDirectoryResolver`.

#### Scenario: Versiones en Directory.Packages.props
- **WHEN** se busca `Avalonia` en `Directory.Packages.props`
- **THEN** existe una entrada `PackageVersion` con version 11.3.x

#### Scenario: Referencias en el csproj
- **WHEN** se inspecciona `GitExtensions.Avalonia.csproj`
- **THEN** contiene `PackageReference` para `Avalonia`, `Avalonia.Desktop`,
  `Avalonia.Themes.Fluent` y `CommunityToolkit.Mvvm`
- **AND** contiene `ProjectReference` a `GitExtUtils`
- **AND** contiene `ProjectReference` a `GitCommands`

#### Scenario: VS-Threading disponible via referencia global
- **WHEN** se compila el proyecto
- **THEN** `Microsoft.VisualStudio.Threading` esta disponible sin `PackageReference`
  explicito en el csproj

### Requirement: Sin dependencias de WinForms
El proyecto `GitExtensions.Avalonia` SHALL NOT referenciar `GitUI`,
`GitExtUtils.WinForms` ni ningun ensamblado con dependencia de WinForms fuera de
`GitExtUtils` y `GitCommands` (ambos cross-platform, `net10.0`, `UseWindowsForms=false`).
SHALL compilar como `net10.0` puro sin `UseWindowsForms`.

#### Scenario: Sin dependencias WinForms
- **WHEN** se inspeccionan las referencias de `GitExtensions.Avalonia.csproj`
- **THEN** no hay `ProjectReference` a `GitUI`, `GitExtUtils.WinForms` ni
  ningun proyecto con `UseWindowsForms=true`

#### Scenario: Compilacion multiplataforma
- **WHEN** se compila el proyecto
- **THEN** el TFM efectivo es `net10.0` (sin `-windows`) y `UseWindowsForms` es `false`

#### Scenario: Referencia a GitExtUtils y GitCommands permitida
- **WHEN** se inspecciona `GitExtensions.Avalonia.csproj`
- **THEN** contiene `ProjectReference` a `GitExtUtils` y `GitCommands` (ambos cross-platform)

### Requirement: Textos de la shell mediante localización

Los textos visibles añadidos o modificados en la shell Avalonia SHALL resolverse mediante la
capacidad `avalonia-localization`, incluyendo textos de bienvenida, apertura de repositorio,
estados, errores y comandos.

#### Scenario: No hay literales de presentación para textos localizables

- **WHEN** se inspeccionan las vistas y ViewModels de la shell
- **THEN** los textos visibles localizables se obtienen mediante claves o bindings de recursos
- **AND** los literales restantes se limitan a valores técnicos que no se presentan al usuario

#### Scenario: La shell conserva su estado al cambiar idioma

- **WHEN** se cambia la cultura mientras la shell muestra bienvenida o un repositorio abierto
- **THEN** se actualizan los textos visibles
- **AND** no se pierde la navegación ni el repositorio seleccionado
