// This add-in software provided by ZGF Architects LLP is for
// illustrative purposes only.  
// This software is supplied "AS IS" without any warranties and support.

using System;
using System.Windows.Forms;

namespace ZGF
{
    /// <summary>
    /// Dialog interface for viewport renumbering add-in.
    /// </summary>
    public partial class ChooseNumberingPattern : Form
    {
        // Private member variable for storing Revit CommandData in dialog class. 
        // This provides and entry point to access the Revit application, active 
        // document and other resources. 
        private Autodesk.Revit.UI.ExternalCommandData m_commandData;

        /// <summary>
        /// Constructor for the form.
        /// </summary>
        /// <param name="commandData">The Autodesk.Revit.UI.ExternalCommandData object.</param>
        public ChooseNumberingPattern(Autodesk.Revit.UI.ExternalCommandData commandData)
        {
            InitializeComponent();

            m_commandData = commandData;
            
            // Dialog caption
            this.Text = "Renumber Views on Sheet";

            // Adds images from project resources to image controls placed on the main form.
            // These image controls serve as buttons. 
            pictureBoxDownLeft.Image = ZGF.Properties.Resources.Renumber_Down_Left_32;
            pictureBoxLeftDown.Image = ZGF.Properties.Resources.Renumber_Left_Down_32;
            pictureBoxDownRight.Image = ZGF.Properties.Resources.Renumber_Down_Right_32;
            pictureBoxRightDown.Image = ZGF.Properties.Resources.Renumber_Right_Down_32;
        }

        /// <summary>
        /// Closes the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #region User Click Event-handlers

        /// <summary>
        /// Constructs an instance of ZGF.RenumberViewsCommands and then
        /// renumbers the viewports on the active sheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxDownLeft_Click(object sender, EventArgs e)
        {
            ZGF.RenumberViewsCommandHelper cmd = new RenumberViewsCommandHelper(m_commandData);            
            cmd.RenumberViewsBySortDirection(SortDirection.DownThenLeft);
            if (checkBoxCloseOnSort.Checked)
                this.Close();
        }

        /// <summary>
        /// Constructs an instance of ZGF.RenumberViewsCommands and then
        /// renumbers the viewports on the active sheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxLeftDown_Click(object sender, EventArgs e)
        {
            ZGF.RenumberViewsCommandHelper cmd = new RenumberViewsCommandHelper(m_commandData);
            cmd.RenumberViewsBySortDirection(SortDirection.LeftThenDown);
            if (checkBoxCloseOnSort.Checked)
                this.Close();
        }

        /// <summary>
        /// Constructs an instance of ZGF.RenumberViewsCommands and then
        /// renumbers the viewports on the active sheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxDownRight_Click(object sender, EventArgs e)
        {
            ZGF.RenumberViewsCommandHelper cmd = new RenumberViewsCommandHelper(m_commandData);
            cmd.RenumberViewsBySortDirection(SortDirection.DownThenRight);
            if (checkBoxCloseOnSort.Checked)
                this.Close();
        }

        /// <summary>
        /// Constructs an instance of ZGF.RenumberViewsCommands and then
        /// renumbers the viewports on the active sheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxRightDown_Click(object sender, EventArgs e)
        {
            ZGF.RenumberViewsCommandHelper cmd = new RenumberViewsCommandHelper(m_commandData);
            cmd.RenumberViewsBySortDirection(SortDirection.RightThenDown);
            if (checkBoxCloseOnSort.Checked)
                this.Close();
        }

        // These eventhandlers make the labels do the same thing as when clicking the
        // by passing the appropriate image control's eventhandler.
        private void labelDowLeft_Click(object sender, EventArgs e)
        {
            pictureBoxDownLeft_Click(new object(), new EventArgs());
        }

        private void labelLeftDown_Click(object sender, EventArgs e)
        {
            pictureBoxLeftDown_Click(new object(), new EventArgs());
        }

        private void labelDownRight_Click(object sender, EventArgs e)
        {
            pictureBoxDownRight_Click(new object(), new EventArgs());
        }

        private void labelRightDown_Click(object sender, EventArgs e)
        {
            pictureBoxRightDown_Click(new object(), new EventArgs());
        }
        #endregion
    }
}
