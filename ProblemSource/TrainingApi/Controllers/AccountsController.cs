using Microsoft.AspNetCore.Mvc;
using ProblemSource.Models;
using TrainingApi.Services;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly OldDbRaw oldDb;
        private readonly ILogger<AggregatesController> _logger;

        public AccountsController(OldDbRaw oldDb, ILogger<AggregatesController> logger)
        {
            this.oldDb = oldDb;
            _logger = logger;
        }

        [HttpPost]
        public Task<Account> Post(AccountCreateDTO dto)
        {
            return Task.FromResult(new Account());
        }

        [HttpPut]
        public Task<Account> Put(AccountCreateDTO dto)
        {
            return Task.FromResult(new Account());
        }

        public class AccountCreateDTO
        {
            public string TrainingPlan { get; set; } = "";
            public TrainingSettings TrainingSettings { get; set; } = new TrainingSettings();
        }

        [HttpGet]
        public async Task<IEnumerable<Account>> Get(int skip = 0, int take = 0, string? orderBy = null, bool descending = false)
        {
            var query = $@"
SELECT MAX(other_id) as maxDay, MAX(latest_underlying) as latest, account_id
FROM aggregated_data
WHERE aggregator_id = 2
GROUP BY account_id
{(orderBy == null ? "" : "ORDER BY " + orderBy + " " + (descending ? "DESC" : "ASC"))}
OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY
";
            var result = await oldDb.Read(query, (reader, columns) => new Account { NumDays = reader.GetInt32(0), Latest = reader.GetDateTime(1), Id = reader.GetInt32(2) });
            return result;
        }

        public class Account
        {
            public int Id { get; set; }
            public int NumDays { get; set; }
            public DateTime Latest { get; set; }
        }
    }

    public class ConfirmationDialog
    {
        public PlainText title { get; set; } = new();
        public Text text { get; set; } = new();
        public PlainText confirm { get; set; } = new();
        public PlainText deny { get; set; } = new();
        public string? style { get; set; }
    }

    public class Option
    {
        // https://api.slack.com/reference/messaging/composition-objects#option
        public PlainText text { get; set; } = new(); //TODO: radio buttons and checkboxes can use mrkdwn text objects
        public string value { get; set; } = string.Empty;
        public PlainText? description { get; set; }
        public string? url { get; set; }
    }

    public abstract class ElementBase
    {
        public abstract string type { get; }
    }

    public class Button : ElementBase
    {
        public override string type => "button";
        public PlainText text { get; set; } = new();
        public string action_id { get; set; } = string.Empty;
        public string? url { get; set; }
        public string? value { get; set; }
        public string? style { get; set; }
        public ConfirmationDialog? confirm { get; set; }
        public string? accessibility_label { get; set; }
    }

    public class CheckBoxGroup : ElementBase
    {
        public override string type => "checkboxes";
        public string action_id { get; set; } = string.Empty;
        public List<Option> options { get; set; } = new();
        public List<Option>? initial_options { get; set; }
        public ConfirmationDialog? confirm { get; set; }
        public bool? focus_on_load { get; set; }
    }


    public abstract class BlockBase
    {
        public abstract string type { get; }
        public string? block_id { get; set; }
    }

    public class Divider : BlockBase
    {
        public override string type => "divider";
    }
    public class Actions : BlockBase
    {
        public override string type => "actions";
        public List<ElementBase> elements { get; set; } = new();
    }

    public class Header : BlockBase
    {
        public override string type => "header";
        public Text text { get; set; } = new();
    }

    public class Input : BlockBase
    {
        public Input(ElementBase element)
        {
            this.element = element;
        }
        public override string type => "input";
        public Text label { get; set; } = new();
        public ElementBase element { get; set; }
        public bool? dispatch_action { get; set; }
        public PlainText? hint { get; set; }
        public bool? optional { get; set; }
    }

    public class Section : BlockBase
    {
        public override string type => "section";
        public Text? text { get; set; }
        public List<Text>? fields { get; set; }
        // TODO: Validate either text of fields
        public ElementBase? accessory { get; set; } // https://api.slack.com/reference/block-kit/block-elements
    }

    public enum TextType
    {
        plain_text,
        mrkdwn
    }

    public abstract class TextBase
    {
        public string text { get; set; } = string.Empty;
        public bool? emoji { get; set; }
        public bool? verbatim { get; set; }
    }
    public class Text : TextBase
    {
        public TextType type { get; set; } = TextType.mrkdwn;
    }
    public class PlainText : TextBase
    {
        public TextType type => TextType.plain_text;
    }

}