# Converte o boneco do Blender para o Godot, sem esqueleto (mesh estática).
#
# Uso (no terminal, a partir da pasta do projeto):
#   "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b modelos\fonte\BASEmodel.blend ^
#       --python ferramentas\blender\exportar_boneco.py -- modelos\boneco.glb
#
# Por que sem esqueleto: o osso raiz do BASEmodel entra no Godot girado ~93° e o boneco
# aparece deitado. Enquanto não houver animações, o esqueleto não é necessário.
import bpy
import sys

saida = sys.argv[sys.argv.index("--") + 1]
corpo = bpy.data.objects["Cube"]

# Tira as ligações de esqueleto (o arquivo tem 3 vazias e 1 real) e aplica o espelhamento.
for m in list(corpo.modifiers):
    if m.type == "ARMATURE":
        corpo.modifiers.remove(m)
bpy.context.view_layer.objects.active = corpo
for o in bpy.context.view_layer.objects:
    o.select_set(False)
corpo.select_set(True)
for m in list(corpo.modifiers):
    bpy.ops.object.modifier_apply(modifier=m.name)

bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
for nome in ("Armature", "Camera", "Light"):
    if nome in bpy.data.objects:
        bpy.data.objects.remove(bpy.data.objects[nome])
corpo.name = "Boneco"

bpy.ops.export_scene.gltf(
    filepath=saida,
    export_format="GLB",
    use_selection=True,
    export_apply=True,
    export_skins=False,
    export_animations=False,
    export_yup=True,
)
print("EXPORTADO", saida, "dimensoes", tuple(round(v, 2) for v in corpo.dimensions))
