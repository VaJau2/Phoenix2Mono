import bpy

# Используется в блендере для переименования костей из Leg1B в leg_1_b

D = bpy.data
arm = D.armatures[0]
for bone in arm.bones:
    oldName = bone.name.lower()
    newName = ""
    counter = 0
    for i, char in enumerate(oldName):
        if (counter == 0) and (char == 'b' and oldName[i+1] == 'l'):
            newName += char + "_"
        elif char.isnumeric():
            newName += "_" + char
        elif char == '.':
            newName += "_"
        else:
            newName += char
    bone.name = newName
    