VB6 Visual Mockup Designer

Herramienta de prototipado moderno para sistemas heredados.

Este proyecto es una aplicación de escritorio desarrollada en WPF (C#) diseñada para modernizar y agilizar el proceso de diseño de interfaces de usuario para Visual Basic 6. Sustituye el uso de editores de imagen estáticos (como Paint) por un entorno de objetos dinámico y editable.

🚀 Funcionalidades Principales

1. Entorno de Diseño "Drag & Drop"

Caja de Herramientas (Toolbox): Réplica de los controles estándar de VB6 (CommandButton, TextBox, Label, Frame, etc.).

Lienzo Inteligente: Sistema de cuadrícula (Snap-to-Grid) que ajusta los controles cada 8 píxeles para una alineación perfecta.

Estética Fiel: Los controles imitan el estilo visual de Windows 98/2000 para garantizar que el mockup se vea como el producto final.

2. Ciclo de Ingeniería Inversa

Importador de .FRM: Capaz de leer archivos de formulario reales de VB6 y reconstruir la interfaz visualmente en segundos.

Conversión Automática: Traduce automáticamente las coordenadas de Twips (VB6) a Píxeles (WPF) y viceversa.

3. Exportación y Documentación

Generación de Código: Exporta el diseño modificado a un archivo .frm limpio, listo para copiar y pegar en el IDE de Visual Basic.

Exportar a Imagen: Genera archivos PNG del diseño con un solo clic para compartir por correo o Teams sin necesidad de recortes manuales.

🛠️ Requisitos Técnicos

Sistema Operativo: Windows 10 o superior.

Framework: .NET 6.0 o superior (incluido en .NET Desktop Runtime).

IDE Recomendado: Visual Studio 2022 Community.

📦 Instalación y Uso

Clonar el repositorio o copiar la carpeta del proyecto.

Abrir la solución VB6MockupDesigner.sln en Visual Studio.

Compilar y ejecutar (F5).

Para empezar:

Arrastra controles desde la izquierda al centro.

Usa el panel derecho para cambiar nombres y textos.

Usa el menú superior para Importar o Exportar.

🔮 Próximos Pasos (Roadmap)

Implementado:

[x] Implementar redimensionamiento de controles con el ratón (Resizing handles).

[x] Selección múltiple de objetos.

[x] Historial (Undo/Redo).

[x] Portapapeles (Copy/Paste/Cut).

[x] Soporte para menús y barras de estado.

[x] Herramientas de Alineación: Barra de herramientas para alinear (Izquierda, Arriba, Centro), igualar tamaño y distribuir espacio entre objetos.

Pendiente:


[ ] Orden Z (Capas): Funcionalidad para "Traer al frente" y "Enviar al fondo" (útil para Frames y fondos).

[ ] Contenedores Reales (Parenting): Lógica para que al soltar un control sobre un Frame, este se convierta en su hijo y se muevan juntos.

[ ] Gestión de Proyectos (.VBP): Explorador de soluciones para manejar múltiples formularios y módulos simultáneamente.

[ ] Editor de TabIndex: Modo visual para establecer el orden de tabulación haciendo clic en los controles secuencialmente.

[ ] Reglas y Guías: Reglas en los bordes con medidas en Twips y líneas guía arrastrables para diseño de precisión.

[ ] Modo Simulación: Vista previa interactiva que oculta la rejilla y permite escribir en inputs o probar clicks sin editar.