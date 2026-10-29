---
name: unity-editor-handoff
description: Cómo crear prefabs, escenas y configuración de Unity sin abrir el Editor, y cómo darme los pasos manuales. Úsala cuando una tarea requiera algo hecho dentro del Editor.
---

# Trabajar con Unity sin el Editor

No puedes abrir el Editor. Tampoco debes editar a mano archivos serializados de Unity.

## No edites a mano

- `.unity` (escenas), `.prefab`, `.asset`, `.mat`, `.controller`, `.meta`.
- `ProjectSettings/*` y `Packages/manifest.json`, salvo que te lo pida.

Un error de YAML puede romper una escena entera sin que lo notes.

## En su lugar: scripts de Editor

Cuando haya que crear escenas, prefabs, ScriptableObjects o configurar componentes, escribe un script de Editor en `Assets/Editor/` con un menú:

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class UltimaCintaSetup
{
    [MenuItem("Tools/UltimaCinta/Crear GameConfig")]
    public static void CrearGameConfig()
    {
        // crea el asset con AssetDatabase.CreateAsset(...)
    }
}
#endif
```

- Cada menú hace una sola cosa y se puede ejecutar dos veces sin duplicar nada.
- Nómbralos `Tools/UltimaCinta/...`.
- Si algo ya existe, no lo sobrescribas sin avisarme.

## Pasos manuales para mí

Cuando algo no se pueda automatizar (instalar el .unitypackage de Fusion, pegar el App ID, asignar una referencia, hacer el bake del NavMesh, probar en Play), dame una lista **numerada y exacta**:

1. Qué menú o ventana abrir.
2. Qué hacer, con nombres reales de objetos y archivos.
3. Qué debería ver si salió bien.

Sin pasos vagos tipo "configura la cámara".

## Antes de entregar

- Compila con la skill `unity-compile-check`.
- Dime qué escena abrir y qué probar en Play.
