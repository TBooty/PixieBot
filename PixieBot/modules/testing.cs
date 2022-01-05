using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixieBot.modules
{
    public class testing : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
    {
        [ComponentInteraction("role_selection")]
        public async Task RoleSelection(string[] selectedRoles)
        {
            await Context.Interaction.RespondAsync("test");
        }

    }
}
