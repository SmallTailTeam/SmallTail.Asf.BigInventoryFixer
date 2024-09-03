using System.Composition;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;
using ArchiSteamFarm.Steam.Interaction;

namespace SmallTail.Asf.BigInventoryFixer;

[Export(typeof(IPlugin))]
public class BigInventoryFixer : IPlugin, IBotCommand2
{
    public string Name => "SmallTail Big Inventory Fixer";
    public Version Version => typeof(BigInventoryFixer).Assembly.GetName().Version!;

    private readonly List<InventoryFixMethod> _fixMethods = [];
    
    public Task OnLoaded()
    {
        ASF.ArchiLogger.LogGenericInfo($"{Name} loaded");
        
        var fixMethods = new InventoryFixMethods();
        _fixMethods.AddRange([fixMethods.ViaGemPacking, fixMethods.ViaGemUnpacking]);
        
        return Task.CompletedTask;
    }
    
    public Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0)
    {
        return args[0].ToLower() switch
        {
            "fixinventory" => HandleFixInventory(args[1]),
            _ => Task.FromResult<string?>(null)
        };
    }

    private async Task<string?> HandleFixInventory(Bot bot)
    {
        await foreach (var asset in bot.ArchiHandler.GetMyInventoryAsync())
        {
            foreach (var inventoryFixMethod in _fixMethods)
            {
                var fixResult = await inventoryFixMethod(bot, asset);

                if (fixResult is null)
                {
                    continue;
                }

                return Commands.FormatBotResponse(fixResult, bot.BotName);
            }
        }

        return Commands.FormatBotResponse("Failed to find a suitable fix method", bot.BotName);
    }
    
    private async Task<string?> HandleFixInventory(string botNames)
    {
        var bots = Bot.GetBots(botNames);

        if (bots is null || bots.Count <= 0)
        {
            return Commands.FormatStaticResponse($"Bot {botNames} is not found");
        }
        
        var results = await Utilities.InParallel(bots.Select(HandleFixInventory));

        return results.Count == 0 ? null : string.Join(Environment.NewLine, results);
    }
}