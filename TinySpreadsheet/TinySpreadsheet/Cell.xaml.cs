﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TinySpreadsheet.Dependencies;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using TinySpreadsheet.Tokenize;


namespace TinySpreadsheet
{
    /// <summary>
    /// A cell setup for UI with Spreadsheet interaction logic built in.
    /// </summary>
    [Serializable]
    public partial class Cell : UserControl, ISerializable
    {
        ListBox listParent;

        /// <summary>
        /// Gets the DependencyMap held by this Cell.
        /// </summary>
        public DependencyMap Dependencies { get; private set; }

        /// <summary>
        /// Gets or sets this Cell's formula used for evaluation. Setting triggers a Changed event.
        /// </summary>
        public String CellFormula { get; private set; }

        /// <summary>
        /// Gets or sets the display text for this Cell. This is typically the evaluated CellFormula.
        /// </summary>
        public String CellDisplay { get; private set; }

        /// <summary>
        /// Creates a new Cell
        /// </summary>
        public Cell()
        {
            //GUI
            InitializeComponent();
            BorderBrush = new SolidColorBrush(Colors.Black);
            BorderThickness = new Thickness(0.25);
            CellText.MouseDoubleClick += Cell_MouseDoubleClick;
            CellText.LostFocus += CellText_LostFocus;
            CellText.GotFocus += CellText_GotFocus;
            CellText.KeyDown += CellText_KeyDown;

            //!GUI
            CellFormula = "";
        }

        public Cell(SerializationInfo info, StreamingContext context)
        {
            Dependencies = (DependencyMap) info.GetValue("dependencies", typeof (DependencyMap)); // This probably doesn't work
            CellFormula = (String)info.GetValue("formula", typeof(String));
            CellDisplay = (String)info.GetValue("display", typeof(String));
        }

        /// <summary>
        /// Attempts to get the most immediate parent of this Cell of a given type.
        /// </summary>
        /// <param name="t">The type of the parent being searched for.</param>
        /// <returns>The parent with the given type. If no parent of the given type exists, null is returned.</returns>
        FrameworkElement GetParent(Type t)
        {
            FrameworkElement parent = this;

            while ((parent = parent.Parent as FrameworkElement) != null && parent.GetType() != t) ;

            return parent;
        }

        //Custom event
        /// <summary>
        /// Triggered when a change to the value of this Cell occurs.
        /// </summary>
        public event Action<Cell> Changed;
        protected virtual void IChanged()
        {
            if (Changed != null)
                Changed(this);
        }

        //Event subscriptions
        void DependencyChanged(Cell sender)
        {
            throw new NotImplementedException("DependencyChanged not implemented.");
            IChanged();

        }

        void CellText_GotFocus(object sender, RoutedEventArgs e)
        {
            if (listParent == null)
                listParent = GetParent(typeof(ListBox)) as ListBox;
            TextBox t = sender as TextBox;

            listParent.SelectedItems.Add(this);
            if ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) == 0 && (Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) == 0)
                HighlightCleanup();
        }

        void CellText_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox t = sender as TextBox;
            t.IsReadOnly = true;

            if ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) == 0 && (Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) == 0)
                HighlightCleanup();

            ////Save cell state when we lose focuss
            //this.CellFormula = t.Text;
            //cellDisplay = Formula.solve(this).ToString();
            //t.Text = cellDisplay;
        }

        void Cell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextBox t = sender as TextBox;
            t.IsReadOnly = false;
            Keyboard.Focus(t);
            t.Select(0, 0);

            listParent.SelectedItems.Remove(this);
            HighlightCleanup();
        }

        /// <summary>
        /// Clears all highlighted cells except for any that are focused.
        /// </summary>
        void HighlightCleanup()
        {
            List<Cell> cells = new List<Cell>();
            foreach (Cell cell in listParent.SelectedItems)
            {
                if (!cell.CellText.IsFocused)
                    cells.Add(cell);
            }

            foreach (Cell cell in cells)
            {
                listParent.SelectedItems.Remove(cell);
            }
        }

        void CellText_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox t = sender as TextBox;
            if (e.Key == Key.Enter)
            {
                if ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) != 0 || (Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) != 0)
                {
                    t.Text += Environment.NewLine;
                }
                else
                {
                    CellFormula = t.Text;
                    if (CellFormula[0] == '=')
                    {
                        CellDisplay = Formula.Solve(this).ToString();
                    }
                    
                    t.Text = CellDisplay;
                    Dependencies = Tokenizer.GetDependencies(this);
                    Dependencies.SubscribeCallback = DependencyChanged;
                    IChanged();
                }
            }

        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("dependencies", Dependencies, typeof(DependencyMap));
            info.AddValue("formula", CellFormula, typeof(String));
            info.AddValue("display", CellDisplay, typeof(String));
        }

        
    }
}
