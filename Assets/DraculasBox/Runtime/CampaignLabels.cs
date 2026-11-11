using System;
using System.Linq;

namespace DraculasBox.Runtime
{
    public static class CampaignLabels
    {
        public static string Prefix(ChapterCatalog catalog,string id)
        {
            if(catalog?.chapters==null)return "";
            for(int i=0;i<catalog.chapters.Length;i++)
            {
                var chapter=catalog.chapters[i];
                if(!chapter.levels.Any(l=>l.id==id))continue;
                return (i+1)+"-"+new ChapterMapLayout(chapter).numbers[id];
            }
            return "";
        }
        public static string Name(ChapterCatalog catalog,string id,string title)
        {
            string prefix=Prefix(catalog,id);return prefix.Length==0?title:prefix+" "+title;
        }
    }
}
