using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Avanade.Tools.Infrastructure
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Holds the query results object
        /// </summary>
        private SearchResultCollection results;

        /// <summary>
        /// Form constructor
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        #region Event Handlers

        /// <summary>
        /// Event handler for "Run" button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRun_Click(object sender, EventArgs e)
        {
            const string LDAP_PREFIX = "LDAP://";

            #region User-input Validation
            if (String.IsNullOrWhiteSpace(txtConnection.Text))
            {
                MessageBox.Show($"Please, enter the LDAP server address", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtConnection.Focus();
                return;
            }
            if (String.IsNullOrWhiteSpace(txtUser.Text))
            {
                MessageBox.Show($"Please, enter the user name", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtUser.Focus();
                return;
            }
            if (String.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show($"Please, enter the user's password", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Focus();
                return;
            }
            #endregion

            try
            {
                // Change the mouse pointer while the query is being executed
                this.Cursor = Cursors.WaitCursor;

                // Clear any previous result
                ClearResults();

                // Prepare the search object
                var entry = new DirectoryEntry(String.Concat(txtConnection.Text.ToUpper().StartsWith(LDAP_PREFIX) ? "" : LDAP_PREFIX, txtConnection.Text), txtUser.Text, txtPassword.Text);
                var searcher = new DirectorySearcher(entry)
                {
                    PageSize = Byte.MaxValue,
                    Filter = $"({txtQuery.Text})"
                };

                // Execute the search in LDAP server
                results = searcher.FindAll();

                // Verify if there are results
                if (results == null || results.Count == 0)
                {
                    MessageBox.Show($"No results found", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Fill the "Query Results" ListBox
                BindAndSort(lstResults, results);
            }
            catch (LdapException ex)
            {
                MessageBox.Show($"Unable to connect to the LDAP server: {ex.ServerErrorMessage ?? ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch(COMException ex)
            {
                MessageBox.Show($"Error {ex.ErrorCode}: {ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (ArgumentException ex) when (ex.Source == "System.DirectoryServices")
            {
                MessageBox.Show($"Query error: {ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                // Restore the mouse pointer
                this.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Event handler that runs when user clicks on "Exit" buttom
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Event handler that runs when user select an item on "Query Results" ListBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lstResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearProperties();

            SearchResult obj = this.GetSelectedObject();
            
            if (obj == null)
                return;

            BindAndSort(cmbProperty, obj.Properties.PropertyNames);
        }

        /// <summary>
        /// Event handler that runs when user select a property
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbProperty_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearValues();

            SearchResult obj = this.GetSelectedObject();

            if (obj == null)
                return;

            try
            {
                var value = obj.Properties[cmbProperty.Text][0];
                txtValue.Text = value.ToString();
            }
            catch { }
        }

        /// <summary>
        /// Event handler that runs when the applications starts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Fill the "User Account" TextBox with the current logged user
            txtUser.Text = String.IsNullOrWhiteSpace(Environment.UserDomainName) ? Environment.UserName : $"{Environment.UserDomainName}\\{Environment.UserName}";

            // Fill the "LDAP Server" TextBox with the address of the domain control, if there is one
            txtConnection.Text = IPGlobalProperties.GetIPGlobalProperties().DomainName;

            // Define a "default query" that searchs for the current logged user
            txtQuery.Text = $"samAccountName={Environment.UserName}";
        }

        #endregion

        /// <summary>
        /// Return the instance of the LDAP object selected on "Query Results" ListBox
        /// </summary>
        /// <returns></returns>
        private SearchResult GetSelectedObject()
        {
            if (results != null && lstResults.SelectedItem != null)
            {
                foreach (SearchResult result in results)
                {
                    if (lstResults.SelectedItem.Equals(result.Path))
                        return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Load the ListControl with the provided data
        /// </summary>
        /// <param name="control">ListControl to be filled</param>
        /// <param name="data">Data to be loaded</param>
        private void BindAndSort(ListControl control, IEnumerable data)
        {
            var values = new List<string>();
            foreach (var item in data)
            {
                if (item is SearchResult)
                    values.Add((item as SearchResult).Path);
                else
                    values.Add(item.ToString());
            }

            values.Sort();
            control.DataSource = values;
        }

        /// <summary>
        /// Clear the "Value" TextBox
        /// </summary>
        private void ClearValues()
        {
            txtValue.Clear();
            txtValue.Refresh();
        }

        /// <summary>
        /// Clear the "Property" ComboBox
        /// </summary>
        private void ClearProperties()
        {
            ClearValues();
            cmbProperty.DataSource = null;
            cmbProperty.Items.Clear();
            cmbProperty.Refresh();
        }

        /// <summary>
        /// Clear the "Query Results" ListBox
        /// </summary>
        private void ClearResults()
        {
            ClearProperties();
            lstResults.DataSource = null;
            lstResults.Items.Clear();
            results = null;
        }
    }
}