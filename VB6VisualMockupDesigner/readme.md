VB6 Visual Mockup Designer v0.7

Herramienta de prototipado moderno para sistemas heredados.

Este proyecto es una aplicación de escritorio desarrollada en WPF (C#) diseñada para modernizar y agilizar el proceso de diseño de interfaces de usuario para Visual Basic 6. Sustituye el uso de editores de imagen estáticos (como Paint) por un entorno de objetos dinámico y editable.

🚀 Estado Actual (Versión 0.7)

Esta versión incluye el núcleo funcional completo para editar, importar y exportar diseños, junto con herramientas de productividad esenciales.

Funcionalidades Implementadas

Entorno de Diseño: Caja de herramientas completa, lienzo con rejilla (Snap-to-Grid) y panel de propiedades.

Ingeniería Inversa: Importación de archivos .frm existentes con conversión de Twips a Píxeles.

Exportación: Generación de código VB6 válido y exportación a imagen PNG.

Edición Avanzada:

[x] Selección múltiple (Ctrl+Click y Lazo de selección).

[x] Historial de cambios (Deshacer/Rehacer con Ctrl+Z/Ctrl+Y).

[x] Portapapeles (Copiar, Cortar y Pegar).

[x] Redimensionamiento visual con ratón.

Herramientas de Diseño:

[x] Barra de herramientas de Alineación (Izquierda, Centro, Arriba, Igualar Tamaño).

[x] Gestión de Orden Z (Traer al frente / Enviar al fondo).

[x] Soporte para Menús y Barras de Estado.

🛠️ Requisitos Técnicos

Sistema Operativo: Windows 10 o superior.

Framework: .NET 6.0 o superior (incluido en .NET Desktop Runtime).

IDE Recomendado: Visual Studio 2022 Community.

📦 Instalación y Uso

Clonar el repositorio.

Abrir VB6MockupDesigner.sln en Visual Studio.

Compilar y ejecutar (F5).

🔮 Roadmap hacia la v1.0

Para considerar la herramienta completa (v1.0), trabajaremos en las siguientes características pendientes:

[ ] Contenedores Reales (Parenting): Que los controles dentro de un Frame o PictureBox se muevan junto con el contenedor.

[ ] Editor de TabIndex Visual: Interfaz para establecer el orden de tabulación haciendo clic secuencialmente en los controles.

[ ] Reglas y Guías: Reglas en los bordes y líneas guía arrastrables para mayor precisión.

[ ] Explorador de Proyecto: Panel lateral para gestionar múltiples formularios a la vez.

[ ] Modo Simulación: Vista previa sin rejilla para probar la interfaz.

Desarrollado como propuesta de mejora interna para optimizar el flujo de trabajo.