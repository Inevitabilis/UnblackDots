using DevInterface;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using BepInEx;
using static ShortcutColorDevtoolObject.PlacedObjectsManager;
using BepInEx.Logging;
using System;

namespace ShortcutColorDevtoolObject
{
    [BepInPlugin("Inevitabilis.DevtoolObjectShortcutDotsColorifier", "Shortcut dots colorifier", "0.0.3")]
    [BepInDependency("github.notfood.BepInExPartialityWrapper")]
    public class UADRegistration : BaseUnityPlugin //This is the class responsible for registering the object in devtool menu
    {

        public void OnEnable()
        {
            try
            {
                PlacedObjectsManager.plog = Logger;
                RegistermanagedObject<ColorifierUAD, ShortcutColorifierData, ManagedRepresentation>(key: nameof(EnumExt_PlacedObjects.ShortcutColor));
            }
            catch(Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
    public static class EnumExt_PlacedObjects //Not much knowledge on this one, but apparently that's the stuff that EnumExtender needs in order to function properly
                                              //I could rely on POM's catch code in DeclareOrGetEnum, but it's a bad thing to rely on catch code
    {
        public static PlacedObject.Type ShortcutColor;
    }


    public class ColorifierUAD : UpdatableAndDeletable //UAD class, the one responsible for doing stuff
    {
        public ColorifierUAD(PlacedObject owner, Room room)
        {
            data = owner.data as ShortcutColorifierData;
            On.ShortcutGraphics.Draw += ShortcutGraphics_Draw;
            this.room = room;
        }

        private void ShortcutGraphics_Draw(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
        {
            orig(self, timeStacker, camPos);
            Body(self);
        }

        private ShortcutColorifierData data;

        private void Body_old(ShortcutGraphics self)  //The old method of cycling through everything on map. Not too effective, o(x^2) (depends on room size), but it was the simpliest for debug purposes
        {
            try
            {
                if (!WorkInThisRoom(self.camera.room)) return;
                for (int x = 0; x < self.sprites.GetLength(0); x++)
                {
                    for(int y = 0; y < self.sprites.GetLength(1); y++)
                    {
                        if (self.sprites[x,y] == null) continue;
                        if ((new Vector2(x*20, y*20) - data.owner.pos).magnitude < data.radius.magnitude)
                        {
                            self.sprites[x, y].color = new Color(data.red, data.green, data.blue);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                plog.LogError(e);
            }
        }
        private void Body(ShortcutGraphics self) //New method of cycling through dots. o(n) (depends on the amount of shortcuts)
        {
            try
            {
                if (!WorkInThisRoom(self.camera.room)) return;
                if (room?.shortcuts == null) return;
                foreach (ShortcutData shortcutData in room.shortcuts)
                {
                    if (shortcutData.path == null) continue;
                    foreach (RWCustom.IntVector2 shortcut in shortcutData.path)
                    {
                        if (shortcut == null) continue;
                        if (self?.sprites[shortcut.x, shortcut.y] == null) continue;
                        Vector2 worldposition = new Vector2(room.MiddleOfTile(shortcut).x, room.MiddleOfTile(shortcut).y);
                        if ((worldposition - data.owner.pos).magnitude < data.radius.magnitude)
                        {
                            if (self?.sprites[shortcut.x, shortcut.y]?.color == null) continue;
                            self.sprites[shortcut.x, shortcut.y].color = new Color(data.red, data.green, data.blue);
                        }   
                    }
                }
                AccountForCreatures(self);
            }
            catch (Exception e)
            {
                plog.LogError(e);
            }
        }
        private void AccountForCreatures(ShortcutGraphics self) //Vanilla accounting code copypasted into a single function
        {
            foreach (ShortcutHandler.ShortCutVessel shortCutVessel in self.shortcutHandler.transportVessels)
            {
                if (shortCutVessel.room == self.room.abstractRoom && self.sprites[shortCutVessel.pos.x, shortCutVessel.pos.y] != null)
                {
                    self.sprites[shortCutVessel.pos.x, shortCutVessel.pos.y].color = self.ShortCutColor(shortCutVessel.creature, shortCutVessel.pos);
                    if (shortCutVessel.creature.Template.shortcutSegments > 1)
                    {
                        for (int n = 0; n < shortCutVessel.lastPositions.Length; n++)
                        {
                            if (self.sprites[shortCutVessel.lastPositions[n].x, shortCutVessel.lastPositions[n].y] != null)
                            {
                                self.sprites[shortCutVessel.lastPositions[n].x, shortCutVessel.lastPositions[n].y].color = self.ShortCutColor(shortCutVessel.creature, shortCutVessel.lastPositions[n]);
                            }
                        }
                    }
                }
            }
        }
        bool WorkInThisRoom(Room room) //Turns out UAD work as long as they are realized, which is not limited to the room you observe
        {
            return this.room == room;
        }
    }

    public class ShortcutColorifierData : ManagedData //The class responsible for storing data for the object
    {
        public const string redFieldKey = "R";
        public const string greenFieldKey = "G";
        public const string blueFieldKey = "B";

        public ShortcutColorifierData(PlacedObject owner) : base (owner, null)
        {
        }

        [FloatField(redFieldKey, 0f, 1f, 0f, 0.1f, ManagedFieldWithPanel.ControlType.slider, "R")]
        public float red;
        [FloatField(greenFieldKey, 0f, 1f, 0f, 0.1f, ManagedFieldWithPanel.ControlType.slider, "G")]
        public float green;
        [FloatField(blueFieldKey, 0f, 1f, 0f, 0.1f, ManagedFieldWithPanel.ControlType.slider, "B")]
        public float blue;
        [Vector2Field("Radius", defX: 80f, defY: 0f, Vector2Field.VectorReprType.circle)]
        public Vector2 radius;
    }
}
