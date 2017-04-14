namespace Sitecore.Support.Shell.Framework.Commands
{
    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Shell.Applications.Dialogs.LayoutDetails;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Text;
    using Sitecore.Web;
    using Sitecore.Web.UI.Sheer;
    using System;
    using System.Collections.Specialized;

    [Serializable]
    public class SetLayoutDetails : Command
    {
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            Error.AssertObject(context, "context");
            if (context.Items.Length == 1)
            {
                Item item = context.Items[0];
                NameValueCollection parameters = new NameValueCollection
                {
                    ["id"] = item.ID.ToString(),
                    ["language"] = item.Language.ToString(),
                    ["version"] = item.Version.ToString(),
                    ["database"] = item.Database.Name
                };
                Context.ClientPage.Start(this, "Run", parameters);
            }
        }

        public override CommandState QueryState(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            if (context.Items.Length != 1)
            {
                return CommandState.Hidden;
            }
            Item item = context.Items[0];
            if (!item.Locking.HasLock() && !Sitecore.Context.User.IsAdministrator)
            {
                return CommandState.Disabled;
            }
            if (!base.HasField(item, FieldIDs.LayoutField))
            {
                return CommandState.Hidden;
            }
            if (((WebUtil.GetQueryString("mode") != "preview") && item.Access.CanWrite()) && (!item.Appearance.ReadOnly && item.Access.CanWriteLanguage()))
            {
                return base.QueryState(context);
            }
            return CommandState.Disabled;
        }

        protected virtual void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (SheerResponse.CheckModified())
            {
                if (args.IsPostBack)
                {
                    if (args.HasResult)
                    {
                        Database database = Factory.GetDatabase(args.Parameters["database"]);
                        Assert.IsNotNull(database, "Database \"" + args.Parameters["database"] + "\" not found.");
                        Item item = database.GetItem(ID.Parse(args.Parameters["id"]), Language.Parse(args.Parameters["language"]), Sitecore.Data.Version.Parse(args.Parameters["version"]));
                        Assert.IsNotNull(item, "item");
                        LayoutDetailsDialogResult result = LayoutDetailsDialogResult.Parse(args.Result);
                        ItemUtil.SetLayoutDetails(item, result.Layout, result.FinalLayout);
                        if (result.VersionCreated)
                        {
                            Context.ClientPage.SendMessage(this, string.Concat(new object[] { "item:versionadded(id=", item.ID, ",version=", item.Version, ",language=", item.Language, ")" }));
                        }
                    }
                }
                else
                {
                    UrlString str = new UrlString(UIUtil.GetUri("control:LayoutDetails"));
                    str.Append("id", args.Parameters["id"]);
                    str.Append("la", args.Parameters["language"]);
                    str.Append("vs", args.Parameters["version"]);
                    SheerResponse.ShowModalDialog(str.ToString(), "600px", string.Empty, string.Empty, true);
                    args.WaitForPostBack();
                }
            }
        }
    }
}
