using System;
using System.Threading.Tasks;
using Disqord.Bot;
using Qmmands;

namespace Unix.Modules.Parsers
{
    public class GuidTypeParser : DiscordGuildTypeParser<Guid>
    {
        public override ValueTask<TypeParserResult<Guid>> ParseAsync(Parameter parameter, string value, DiscordGuildCommandContext context)
        {
            if (Guid.TryParse(value, out var guid))
            {
                return Success(guid);
            }

            return Failure("Failed to parse Guid.");
        }
    }
}