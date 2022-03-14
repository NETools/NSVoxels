using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.GUI.Macros
{
    public class MacroManager
    {
        private MacroManager()
        {

        }

        private static MacroManager macroManager;
        public static MacroManager GetDefault()
        {
            if (macroManager == null)
                macroManager = new MacroManager();
            return macroManager;
        }

        private Dictionary<Keys, Action<GameTime>> macros = new Dictionary<Keys, Action<GameTime>>();
        
        private bool[] keyStates = new bool[256];
        private List<Keys> singlePressIndices = new List<Keys>();

        public void Update(GameTime gameTime)
        {
            for (int i = 0; i < singlePressIndices.Count; i++)
                if (Keyboard.GetState().IsKeyUp(singlePressIndices[i]))
                {
                    keyStates[(int)singlePressIndices[i]] = false;
                    singlePressIndices.RemoveAt(i);
                }

            for (int i = 0; i < Keyboard.GetState().GetPressedKeyCount(); i++)
            {
                var pressedKey = Keyboard.GetState().GetPressedKeys()[i];
                if (macros.ContainsKey(pressedKey))
                    macros[pressedKey](gameTime);
            }
            
            
        }

        public void DefineMacro(Keys forKey, Action<GameTime> action, bool singlepress = false)
        {
            macros.Add(forKey, new Action<GameTime>((o) =>
            {
                if (singlepress)
                {
                    if (!keyStates[(int)forKey])
                    {
                        action(o);
                        keyStates[(int)forKey] = true;
                        singlePressIndices.Add(forKey);
                    }
                }
                else action(o);
            }));
        }



    }
}
