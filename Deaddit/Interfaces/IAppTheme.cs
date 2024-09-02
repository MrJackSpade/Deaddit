using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Interfaces
{
    public interface IAppTheme
    {
        Color DistinguishedColor { get;  }
        Color DownvoteColor { get;  }
        double FontSize { get;  }
        Color HighlightColor { get;  }
        Color HyperlinkColor { get;  }
        Color PrimaryColor { get;  }
        Color SecondaryColor { get;  }
        Color SubTextColor { get;  }
        Color TertiaryColor { get;  }
        Color TextColor { get;  }
        int ThumbnailSize { get;  }
        Color UpvoteColor { get;  }
    }
}
