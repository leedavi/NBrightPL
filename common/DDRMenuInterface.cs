﻿using System;
using System.Collections.Generic;
using System.Web;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Web.DDRMenu;
using NBrightCore.common;
using NBrightDNN;

namespace NBrightPL.common
{
    public class DdrMenuInterface : INodeManipulator
    {
        #region Implementation of INodeManipulator
        private NBrightDNN.NBrightDataController _objCtrl;

        public List<MenuNode> ManipulateNodes(List<MenuNode> nodes, DotNetNuke.Entities.Portals.PortalSettings portalSettings)
        {
            _objCtrl = new NBrightDataController();
            var settingRecord = _objCtrl.GetByGuidKey(portalSettings.PortalId, -1, "SETTINGS", "NBrightPL");

            var nodeTabList = "*";
            foreach (var n in nodes)
            {
                nodeTabList += n.Text + n.TabId + "*" + n.Breadcrumb + "*";
            }
            var cachekey = "NBrightPL*" + portalSettings.PortalId + "*" + Utils.GetCurrentCulture() + "*" + nodeTabList; // use nodeTablist incase the DDRMenu has a selector.
            var rtnnodes = (List<MenuNode>)Utils.GetCache(cachekey);

            var debugMode = false;
            if (settingRecord != null)
            {
                debugMode = settingRecord.GetXmlPropertyBool("genxml/checkbox/debugmode");
            }
            if (rtnnodes != null && !debugMode) return rtnnodes;

            nodes = BuildNodes(nodes, portalSettings);

            if (settingRecord != null)
            {
                var menuproviders = settingRecord.GetXmlProperty("genxml/textbox/menuproviders");
                var provlist = menuproviders.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (var p in provlist)
                {
                    var prov = CreateProvider(p);
                    if (prov != null)
                    {
                        nodes = prov.ManipulateNodes(nodes, portalSettings);
                    }
                }
            }

            Utils.SetCache(cachekey, nodes);
            return nodes;
        }

        private static INodeManipulator CreateProvider(string providerAssembyClass)
        {
            if (!string.IsNullOrEmpty(providerAssembyClass))
            {
                var prov = providerAssembyClass.Split(Convert.ToChar(","));
                try
                {
                    var handle = Activator.CreateInstance(prov[1], prov[0]);
                    return (INodeManipulator)handle.Unwrap();
                }
                catch (Exception ex)
                {
                    // Error in provider is invalid provider, so remove from providerlist.
                    return null;
                }
            }
            return null;
        }

        private List<MenuNode> BuildNodes(List<MenuNode> nodes, DotNetNuke.Entities.Portals.PortalSettings portalSettings)
        {
            foreach (var n in nodes)
            {
                var dataRecord = _objCtrl.GetByGuidKey(portalSettings.PortalId, -1, "PL", n.TabId.ToString(""));
                if (dataRecord != null)
                {
                    var dataRecordLang = _objCtrl.GetDataLang(dataRecord.ItemID, Utils.GetCurrentCulture());
                    if (dataRecordLang != null)
                    {
                        n.Text = dataRecordLang.GetXmlProperty("genxml/textbox/pagename");
                        n.Keywords = dataRecordLang.GetXmlProperty("genxml/textbox/tagwords");
                        n.Title = dataRecordLang.GetXmlProperty("genxml/textbox/pagetitle");
                        n.Description = dataRecordLang.GetXmlProperty("genxml/textbox/pagedescription");

                        if (n.Children.Count > 0) BuildNodes(n.Children, portalSettings);
                    }
                }
            }
            return nodes;
        }

        #endregion
    }

   
}
