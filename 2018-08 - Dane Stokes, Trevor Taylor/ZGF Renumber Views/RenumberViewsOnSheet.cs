// This add-in software provided by ZGF Architects LLP is for
// illustrative purposes only.  
// This software is supplied "AS IS" without any warranties or support.
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;



namespace ZGF
{

    /// <summary>
    /// This class defines the REVIT COMMAND entry that appears on the Addins tab --> External Commands. 
    /// In order for Revit to load the command, the file "ZGFRenumberViews.addin" needs to be present
    /// in the addins folder with this DLL. 
    /// NOTE: Before running this addin from the Visual Studio debugger,
    /// delete the FILE "ZGFRenumberViews.DLL" to prevent the error "The following module was built either with
    /// optimizations enabled or without debug information:".
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RenumberViewsUI : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Result returnVal = Result.Succeeded;

            ViewSheet activeView = commandData.Application.ActiveUIDocument.ActiveView as ViewSheet;
            if (null != activeView)
            {
                ChooseNumberingPattern dlg = new ChooseNumberingPattern(commandData);
                dlg.ShowDialog();                
            }
            else
            {
                message = "Current Revit view must be a SHEET.";
                returnVal = Result.Failed;
            }

            return returnVal;
        }
    }

    /// <summary>
    /// Class defining a function for renumbering techniques. This class is used by
    /// the form "ChooseNumberingPattern" to pass the user's numbering choice to the
    /// "RenumberViewsOnSheet" class. It also provides an errorhandler in case the current
    /// view is not a sheet, or if something goes wrong during the renumbering function.
    /// </summary>
    public class RenumberViewsCommandHelper
    {
        private ExternalCommandData m_commandData;

        public RenumberViewsCommandHelper(ExternalCommandData commandData)
        {
            m_commandData = commandData;
        }

        public Result RenumberViewsBySortDirection(SortDirection sortDirection)
        {
            // Get a reference to the Active Revit Document
            Document doc = m_commandData.Application.ActiveUIDocument.Document;
            // From the Doc, get a reference to the active View. 
            // Using the following technique will return "null" if the active view is not a sheet.
            ViewSheet activeView = doc.ActiveView as ViewSheet;

            if (null == activeView)
            {

                RenumberViewsOnSheet.RenumberViewsErrorMsg(
                    RenumberViewsResult.CurrentViewMustBeASheet, 
                    "Please activate a sheet view before attempting to renumber views.");
                
                return Result.Cancelled;
            }
            else
            {
                RenumberViewsResult.Result renumberResult = RenumberViewsOnSheet.RenumberViews(doc, sortDirection);
                switch (renumberResult)
                {
                    case RenumberViewsResult.Result.NoViewsOnSheet:
                        RenumberViewsOnSheet.RenumberViewsErrorMsg(
                            RenumberViewsResult.NoViewsOnSheet,
                            "Add some views to the sheet and try again.");
                        return Result.Cancelled;
                    case RenumberViewsResult.Result.SomethingElseScrewedUpTheWorks:
                        RenumberViewsOnSheet.RenumberViewsErrorMsg
                            (RenumberViewsResult.SomethingElseScrewedUpTheWorks,
                            "");
                        return Result.Cancelled;
                    default:
                        return Result.Succeeded;
                }
            }
        }
    }

    
    /// <summary>
    /// Primary programming logic for Revit sheet view renumbering command. 
    /// </summary>
    class RenumberViewsOnSheet
    {
        public static RenumberViewsResult.Result RenumberViews(Document doc, SortDirection sortDirection)
        {
            // Storage for the viewports that have a view title. We're going to sort these:
            List<ViewportWrapper> m_viewsOnSheet = new List<ViewportWrapper>();
            // Storage for the viewports with no visible view title. We're going to skip these:
            List<ViewportWrapper> m_viewsHaveNoTitle = new List<ViewportWrapper>();

            ViewSheet sheet = (ViewSheet)doc.ActiveView;
            
            // Collect Viewports on current sheet that have Detail Numbers
            foreach (ElementId id in sheet.GetAllViewports())
            {
                // Get Viewport on sheet
                Viewport currentVP = (Viewport)doc.GetElement(id);
                // Check to see if it has a 'Detail Number' parameter. If not, it's a schedule or legend, so ignore:
                Parameter pDetailNumber = currentVP.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER) as Parameter;
                // Add to list of Viewports
                if (null != pDetailNumber)
                {                  
                    // Legends have a detail number, but it's an empty string so ignore them:                
                    if (String.IsNullOrEmpty(pDetailNumber.AsString())) 
                        continue;
                    
                    ViewportWrapper vpw = new ViewportWrapper(doc, currentVP);
                    if (vpw.HasTitle)
                        m_viewsOnSheet.Add(vpw);
                    else
                        m_viewsHaveNoTitle.Add(vpw); //These will be appended to m_viewsOnSheet after sorting
                }
            }
                        
            // sort list of viewports using the custom sorting function
            switch (sortDirection)
            {
                case SortDirection.DownThenRight:
                    m_viewsOnSheet.Sort(new RightComparerXthenY());
                    break;
                case SortDirection.RightThenDown:
                    m_viewsOnSheet.Sort(new RightComparerYthenX());
                    break;
                case SortDirection.LeftThenDown:                   
                    m_viewsOnSheet.Sort(new LeftComparerYthenX());
                    break;                
                default: // SortDirection.DownThenLeft
                     m_viewsOnSheet.Sort(new LeftComparerXthenY());                    
                    break;
            }

            if ((m_viewsOnSheet.Count > 0) & (m_viewsHaveNoTitle.Count > 0))
                m_viewsOnSheet.AddRange(m_viewsHaveNoTitle);

            // Any time you modify the Revit document you are required to use a Transaction
            using (Transaction trans = new Transaction(doc, "Renumber views on sheet " + sheet.SheetNumber))
            {
                trans.Start();
                // The elements in the list are now sorted, so just iterate over the list
                // in order beginning from 1 and incrementing its succeeding member by +1.
                try
                {
                    // Append a char to each dtl number so that we can redo the numbering
                    foreach (ViewportWrapper vp in m_viewsOnSheet)
                    {
                        vp.DetailNumber += "_";
                    }

                    // Renumber beginning at 1:
                    int dtlNumber = 1;                                        
                    
                    foreach (ViewportWrapper vp in m_viewsOnSheet)
                    {
                        vp.DetailNumber = dtlNumber.ToString();
                        dtlNumber++;
                    }                    

                    trans.Commit();
                }
                catch
                {
                    return RenumberViewsResult.Result.SomethingElseScrewedUpTheWorks;
                }
                
            }

            return RenumberViewsResult.Result.OK;
        }

        /// <summary>
        /// View renumbering error dialog
        /// </summary>
        /// <returns></returns>
        public static void RenumberViewsErrorMsg(string mainMessageToDisplay, string subTextToDisplayBeneathMain)
        {
            TaskDialog td = new TaskDialog("Renumber Views on Current Sheet");
            td.MainInstruction = mainMessageToDisplay;
            td.MainContent = subTextToDisplayBeneathMain;
            td.TitleAutoPrefix = false;
            td.Show();            
        }

    }

    /// <summary>
    /// This class 'wraps' selected viewport behavior into a convenient package. Wrapper classes 
    /// are a common technique used for code organization. 
    /// </summary>
    class ViewportWrapper
    {
        private Document m_doc;
        // Note: A View is not the same thing as a Viewport: A Viewport is a view that 
        //       has been placed on a sheet.
        private Viewport m_viewport;
        private View m_view;
        private XYZ m_VpOriginPt = XYZ.Zero;

        public ViewportWrapper(Document theDoc, Viewport theViewport)
        {
            m_doc = theDoc;
            m_viewport = theViewport;
            m_view = (View)theDoc.GetElement(m_viewport.ViewId);
            
            try
            {                
                m_VpOriginPt = m_viewport.GetLabelOutline().MinimumPoint;
                HasTitle = true;
            }
            catch
            {
                HasTitle = false; // There is no LabelOutline to get if the viewport type has title set to 'none'                
            } 
        }

        /// <summary>
        /// Reference to the Revit View owned by the Viewport
        /// </summary>
        public View TheView { get { return m_view; } }

        /// <summary>
        /// Origin point of the view
        /// </summary>
        public XYZ ViewOriginPointOnSheet { get { return m_VpOriginPt; }  }

        /// <summary>
        /// Reads or writes the built-in parameter that stores the views detail number
        /// </summary>
        public string DetailNumber
        {
            get 
            {
                Parameter p = m_viewport.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER);
                return p.AsString();
            }
            set
            {
                Parameter p = m_viewport.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER);
                try
                {
                    p.Set((string)value);
                }
                catch
                {
                    throw new Exception("Could not set detail number for view: " + m_view.Name);
                }
            }
        }
       
        public bool HasTitle { get; set; }
    }

    //------------------------------------------------
    // Comparers for sorting view coords
    //------------------------------------------------
    #region Custom Sorting Functions
    /// <summary>
    /// Used by the Sort function of the List object that stored the viewports to be renumbered
    /// </summary>
    class RightComparerXthenY : IComparer<ViewportWrapper>
    {
        public int Compare(ViewportWrapper viewportA, ViewportWrapper viewportB)
        {
            System.Drawing.Point a = new System.Drawing.Point(Convert.ToInt32(viewportA.ViewOriginPointOnSheet.X * 12000), Convert.ToInt32(viewportA.ViewOriginPointOnSheet.Y * 12000));
            System.Drawing.Point b = new System.Drawing.Point(Convert.ToInt32(viewportB.ViewOriginPointOnSheet.X * 12000), Convert.ToInt32(viewportB.ViewOriginPointOnSheet.Y * 12000));

            if (RenumberViewsResult.PointIsEqualTo(b.X, a.X, RenumberViewsResult.FuzzFactorForViewportAlignment)) 
            {   
                return b.Y - a.Y;
            }            
            else
            {                
                return a.X - b.X;
            }
        }
    }
    /// <summary>
    /// Used by the Sort function of the List object that stored the viewports to be renumbered
    /// </summary>
    class RightComparerYthenX : IComparer<ViewportWrapper>
    {
        public int Compare(ViewportWrapper viewportA, ViewportWrapper viewportB)
        {            
            System.Drawing.Point a = new System.Drawing.Point(Convert.ToInt32(viewportA.ViewOriginPointOnSheet.X * 12000), Convert.ToInt32(viewportA.ViewOriginPointOnSheet.Y * 12000));
            System.Drawing.Point b = new System.Drawing.Point(Convert.ToInt32(viewportB.ViewOriginPointOnSheet.X * 12000), Convert.ToInt32(viewportB.ViewOriginPointOnSheet.Y * 12000));

            if (RenumberViewsResult.PointIsEqualTo(a.Y, b.Y, RenumberViewsResult.FuzzFactorForViewportAlignment)) 
            {                
                return a.X - b.X;
            }
            else
            {                
                return b.Y - a.Y;
            }
        }
    }
    
    /// <summary>
    /// Used by the Sort function of the List object that stored the viewports to be renumbered
    /// </summary>
    class LeftComparerXthenY : IComparer<ViewportWrapper>
    {
        public int Compare(ViewportWrapper viewportA, ViewportWrapper viewportB)
        {   
            System.Drawing.Point a = new System.Drawing.Point(Convert.ToInt32(viewportA.ViewOriginPointOnSheet.X * 12000), Convert.ToInt32(viewportA.ViewOriginPointOnSheet.Y * 12000));
            System.Drawing.Point b = new System.Drawing.Point(Convert.ToInt32(viewportB.ViewOriginPointOnSheet.X * 12000), Convert.ToInt32(viewportB.ViewOriginPointOnSheet.Y * 12000));
            int returnval;
            if (RenumberViewsResult.PointIsEqualTo(b.X, a.X, RenumberViewsResult.FuzzFactorForViewportAlignment)) 
            {
                returnval = b.Y - a.Y;
            }
            else
            {
                returnval = b.X - a.X;
            }
            return returnval;
        }
    }
    /// <summary>
    /// Used by the Sort function of the List object that stored the viewports to be renumbered
    /// </summary>
    class LeftComparerYthenX : IComparer<ViewportWrapper>
    {
        public int Compare(ViewportWrapper viewportA, ViewportWrapper viewportB)
        {         
            System.Drawing.Point a = new System.Drawing.Point(Convert.ToInt32(viewportA.ViewOriginPointOnSheet.X * 12000), Convert.ToInt32(viewportA.ViewOriginPointOnSheet.Y * 12000)); // x 12000 converts to 1000's of an inch
            System.Drawing.Point b = new System.Drawing.Point(Convert.ToInt32(viewportB.ViewOriginPointOnSheet.X * 12000), Convert.ToInt32(viewportB.ViewOriginPointOnSheet.Y * 12000));

            if (RenumberViewsResult.PointIsEqualTo(b.Y, a.Y, RenumberViewsResult.FuzzFactorForViewportAlignment)) 
            {
                return b.X - a.X;
            }
            else
            {
                return b.Y - a.Y;
            }
        }
    }

    

    #endregion
    //------------------------------------------------
    
    public enum SortDirection
    {        
        LeftThenDown,
        RightThenDown,
        DownThenLeft,
        DownThenRight
    }

    class RenumberViewsResult
    {
        public enum Result
        {
            OK,
            NoViewsOnSheet,
            SomethingElseScrewedUpTheWorks,
            CurrentViewMustBeASheet

        }

        public const string NoViewsOnSheet = "Current sheet has no views";
        public const string SomethingElseScrewedUpTheWorks = "Unspecified error";
        public const string CurrentViewMustBeASheet = "Current view must be a sheet";
        public const int FuzzFactorForViewportAlignment = 1000;

       
        // Comparer function with fuzz factor
        public static bool PointIsEqualTo(int P1, int P2, int fuzzAmtMilliInches)
        {
            return (Math.Abs(P1 - P2) < fuzzAmtMilliInches);
        }
    }


}
