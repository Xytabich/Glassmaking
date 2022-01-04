using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.GenericItemAction
{
    public abstract class GuiHeldItemActionDialog : GuiDialogGeneric
    {
        private GenericItemActionSystem system;

        public GuiHeldItemActionDialog(string DialogTitle, ICoreClientAPI capi) : base(DialogTitle, capi)
        {
            system = capi.ModLoader.GetModSystem<GenericItemActionSystem>();
        }

        protected void DoItemAction(IPlayer player, CollectibleObject item, string action, ITreeAttribute attributes)
        {
            IGenericHeldItemAction heldAction;
            if((heldAction = item as IGenericHeldItemAction) != null)
            {
                if(heldAction.GenericHeldItemAction(player, action, attributes))
                {
                    system.SendActionMessage(item.Id, action, attributes);
                    return;
                }
            }
            foreach(var behavior in item.CollectibleBehaviors)
            {
                if((heldAction = behavior as IGenericHeldItemAction) != null)
                {
                    if(heldAction.GenericHeldItemAction(player, action, attributes))
                    {
                        system.SendActionMessage(item.Id, action, attributes);
                        return;
                    }
                }
            }
        }
    }
}