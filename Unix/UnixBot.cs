using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Sharding;
using Disqord.Rest;
using Disqord.Sharding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;
using Serilog;
using Unix.Common;
using Unix.Modules.Parsers;
using Unix.Services.Core;

namespace Unix
{
    public class UnixBot : DiscordBotSharder
    {
        private readonly GuildService _guildService;
        public UnixBot(GuildService guildService, IOptions<DiscordBotSharderConfiguration> options, ILogger<DiscordBotSharder> logger, IServiceProvider services, DiscordClientSharder client) : base(options, logger, services, client)
        {
            _guildService = guildService;
        }

        private UnixConfiguration UnixConfig = new();
        protected override LocalMessage FormatFailureMessage(DiscordCommandContext context, FailedResult result)
        {
            var prefix = _guildService.FetchGuildPrefixAsync(context.GuildId.Value).GetAwaiter().GetResult();
            if (result is ChecksFailedResult checksFailedResult)
            {
                var check = checksFailedResult.FailedChecks.ElementAt(0);
                return new LocalMessage()
                    .WithContent($"⚠ {check.Result.FailureReason}");

            }
            if (result is OverloadsFailedResult overloadsFailedResult)
            {
                string failureReason = this.FormatFailureReason(context, result);
                Log.Logger.Error(failureReason);
                static string FormatParameter(Parameter parameter)
                {
                    string format;
                    if (parameter.IsMultiple)
                    {
                        format = "{0}[]";
                    }
                    else
                    {
                        format = parameter.IsRemainder
                            ? "{0}..."
                            : "{0}";
                        format = parameter.IsOptional
                            ? $"[{format}]"
                            : $"<{format}>";
                    }

                    return string.Format(format, parameter.Name);
                }

                var builder = new StringBuilder();
                builder.AppendLine($"The input given doesn't match any overloads.\nAvailable overloads:");
                foreach (var (overload, overloadResult) in overloadsFailedResult.FailedOverloads)
                {
                    var overloadReason = base.FormatFailureReason(context, overloadResult);
                    if (overloadReason == null)
                        continue;
                    //if (overloadResult is ChecksFailedResult cfr)
                    //{
                    //    builder.Append(overloadReason);
                    //    return new LocalMessage()
                    //        .WithContent(builder.ToString());
                    //}
                    builder.AppendLine($"`{prefix}{overload.FullAliases[0]} {string.Join(' ', overload.Parameters.Select(FormatParameter))}`");
                }

                return new LocalMessage()
                    .WithContent(builder.ToString());
            }

            if (result is ArgumentParseFailedResult argumentParseFailedResult)
            {
                static string FormatParameter(Parameter parameter)
                {
                    string format;
                    if (parameter.IsMultiple)
                    {
                        format = "{0}[]";
                    }
                    else
                    {
                        format = parameter.IsRemainder
                            ? "{0}..."
                            : "{0}";
                        format = parameter.IsOptional
                            ? $"[{format}]"
                            : $"<{format}>";
                    }

                    return string.Format(format, parameter.Name);
                }

                var builder = new StringBuilder();
                var overloadReason = base.FormatFailureReason(context, argumentParseFailedResult);
                builder.AppendLine($"⚠ Command: `{prefix}{argumentParseFailedResult.Command.FullAliases[0]} {string.Join(' ', argumentParseFailedResult.Command.Parameters.Select(FormatParameter))}`\n{overloadReason}");
                return new LocalMessage()
                    .WithContent(builder.ToString());
            }
            if (result is CommandExecutionFailedResult commandFailedResult)
            {
                if (commandFailedResult.Exception.Message == "Blacklisted.") return null;
                return new LocalMessage()
                    .WithContent($"⚠ {commandFailedResult.Exception.Message}");
            }
            if (result is CommandNotFoundResult commandNotFoundResult)
            {
                return new LocalMessage()
                    .WithContent($"⚠ Command not found.");
            }
            Log.Logger.Error($"{result.FailureReason}");
            return new LocalMessage()
                .WithContent("There was an error just now, please check the inner exception for more details.");
        }

        protected override async ValueTask HandleFailedResultAsync(DiscordCommandContext context, FailedResult result)
        {
            await base.HandleFailedResultAsync(context, result);
        }

        protected override async ValueTask AddTypeParsersAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            Commands.AddTypeParser(new TimeSpanParser());
            Commands.AddTypeParser(new PrefixTypeParser());
            Commands.AddTypeParser(new GuidTypeParser());
            await base.AddTypeParsersAsync(cancellationToken);
        }
    }
}