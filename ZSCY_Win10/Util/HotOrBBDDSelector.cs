﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ZSCY_Win10.Data.Community;

namespace ZSCY_Win10.Util
{
    public class HotOrBBDDSelector: DataTemplateSelector
    {
        public DataTemplate BBDDTemplate { get; set; }
        public DataTemplate nBBDDTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(System.Object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;
            if (element != null && item != null)
            {
                HotFeed h = item as HotFeed;
                if (h != null)
                {
                    if (h.content.content != null)
                    {
                        h.content.contentbase = new JWZXFeeds { content = h.content.content };
                    }
                    return nBBDDTemplate;
                }
                else return BBDDTemplate;
            }
            return null;
        }
    }
}
