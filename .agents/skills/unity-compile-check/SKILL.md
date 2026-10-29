---
name: unity-compile-check
description: Compila el proyecto Unity por consola (batchmode) y lee los errores de C#. Úsala siempre antes de entregar código o decir que una tarea está lista.
---

# Compilar Unity por consola

Codex no puede abrir el Editor, pero Unity puede compilar en modo batch y escribir un log. Hazlo antes de dar una tarea por terminada.

## Cuándo usarla

- Después de crear o modificar cualquier script C#.
- Antes de hacer commit de una tarea o fase.
- Cuando yo diga que "hay errores" en Unity.

## Importante

- Si el Editor de Unity está abierto con este proyecto, el modo batch falla porque el proyecto está bloqueado. En ese caso, pídeme que cierre Unity o que me pase la consola de errores del Editor.
- Ruta por defecto del Editor en Windows:
  `C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe`
  Si no existe, pregúntame la ruta real; no adivines otra versión.

## Comando (PowerShell)

Ejecútalo desde dentro del repo. La raíz del repo es la raíz del proyecto Unity (hoy `D:\2026-II\REALIDAD VIRTUAL\ULTIMACINTA\My project`, con espacios: por eso las comillas).

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe"
$project = (git rev-parse --show-toplevel).Replace("/", "\")
$log = "$project\Logs\compile-check.log"
New-Item -ItemType Directory -Force "$project\Logs" | Out-Null
& $unity -batchmode -nographics -quit -projectPath "$project" -logFile "$log"
$exit = $LASTEXITCODE
Write-Host "Código de salida: $exit"
Select-String -Path $log -Pattern "error CS\d+" | Select-Object -First 30
```

## Cómo interpretar

- Código de salida `0` y sin líneas `error CS`: compila.
- Cualquier `error CS####`: corrígelo y vuelve a compilar. Repite hasta que no haya errores.
- Los warnings no bloquean, pero menciona los que vengan de tu código.
- Si el log dice que el proyecto está abierto en otra instancia, no es un error de código: avísame.

## Qué reportarme

Una línea: "Compila sin errores" o la lista corta de errores restantes con archivo y línea. No pegues el log completo.
