using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;

namespace Deaddit.Interfaces
{
    internal interface IHasChildren
    {
        IAppNavigator AppNavigator { get; }

        BlockConfiguration BlockConfiguration { get; }

        Layout ChildContainer { get; }

        ApiPost Post { get; }

        SelectionGroup SelectionGroup { get; }

        void InitChildContainer();

        void MoreCommentsClick(object? sender, IMore e);
    }
}