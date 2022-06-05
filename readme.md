# Unity terrain backup / painting restore 

This is a quick proof of concept of doing a terrain backup and allowing the restore to be painted back as required.

- It backups trees, details, textures and heights. Prefabs are not backed up! It does basic remapping on texture/detail/tree prototype order changes on restore, but will not (as of yet) add missing prototypes back in.
- To use place a empty gameobject and attach the TerrainPaintRestorer script. Add your terrain gameobjects. Click "Backup Terrain". Do some changes to your terrain(s). Click "Paint Restore" and draw on the terrain to restore the specific area.

This is just a proof of concept - if it breaks your terrain don't blame me!

Project was done in Unity 2021.3.2f1

---
#### Free assets used

Nature Starter Kit: https://assetstore.unity.com/packages/3d/environments/nature-starter-kit-2-52977
