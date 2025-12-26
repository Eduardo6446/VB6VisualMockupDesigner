VB6 Visual Mockup Designer v0.8 (Beta)

Herramienta de prototipado moderno para sistemas heredados.

Este proyecto es una aplicación de escritorio desarrollada en WPF (C#) diseñada para modernizar y agilizar el proceso de diseño de interfaces de usuario para Visual Basic 6. Sustituye el uso de editores de imagen estáticos (como Paint) por un entorno de objetos dinámico y editable.

🚀 Estado Actual (Versión 0.8)

Esta versión introduce herramientas avanzadas de productividad y gestión de archivos.

Funcionalidades Implementadas

1. Entorno de Diseño

[x] Caja de Herramientas Completa: Controles estándar de VB6.

[x] Lienzo Inteligente: Cuadrícula (Snap-to-Grid) de 8px.

[x] Panel de Propiedades: Edición de Nombre, Texto/Caption y Posición.

2. Edición y Productividad

[x] Selección Múltiple: Mediante Ctrl+Click y Lazo de selección.

[x] Historial: Deshacer/Rehacer completo (Ctrl+Z, Ctrl+Y).

[x] Portapapeles: Copiar, Cortar y Pegar controles manteniendo propiedades.

[x] Alineación: Barra de herramientas para alinear (Izquierda, Centro, etc.) e igualar tamaños.

[x] Orden Z: Traer al frente y enviar al fondo.

3. Modos Especiales

[x] Modo Simulación (F5): Vista limpia sin rejilla que permite interactuar con los controles (escribir, clicar).

[x] Editor de TabIndex Visual: Herramienta para asignar el orden de tabulación haciendo clic secuencialmente en los controles.

4. Archivos e Integración

[x] Importar .FRM: Carga formularios existentes de VB6.

[x] Generar Código: Exporta el diseño a código .frm válido.

[x] Exportar Imagen: Genera un PNG del diseño actual.

⚠️ Problemas Conocidos (Known Issues) - Próximo Sprint

Estas son las limitaciones identificadas en la v0.8 que se abordarán en futuras actualizaciones:

TabIndex en TextBoxes: Al usar la herramienta de TabIndex sobre un TextBox, a veces el control intenta capturar el texto en lugar de asignar el índice. Workaround: Hacer clic en el borde del control.

Contenedores (Parenting): Actualmente, los controles se colocan visualmente sobre los Frames/PictureBoxes, pero no se convierten en "hijos" lógicos. Al mover el Frame, los controles internos no se mueven con él.

Importación Anidada: El importador lee todos los controles, pero los coloca en el nivel raíz del formulario, perdiendo la jerarquía original de contenedores.

🛠️ Requisitos Técnicos

Sistema Operativo: Windows 10 o superior.

Framework: .NET 6.0 o superior (.NET Desktop Runtime).

IDE: Visual Studio 2022.

📦 Instalación

Clonar el repositorio.

Abrir VB6MockupDesigner.sln.

Compilar en modo Release.

Ejecutar VB6MockupDesigner.exe.
