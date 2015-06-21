﻿/*  
 * Percolator Analysis Services
 *  Copyright (c) 2014 CoopDIGITy
 *  Author: Matthew Hallmark
 *  A Copy of the Liscence is included in the "AssemblyInfo.cs" file.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AnalysisServices.AdomdClient;
using Percolator.AnalysisServices.Linq;

namespace Percolator.AnalysisServices
{
    /// <summary>
    /// The Percolator Analysis Services IQueryProvider implementation.
    /// </summary>
    public class Providerlator : IMdxProvider
    {
        AdomdConnection _connection;

        public Providerlator(string connectionString)
        {
            this._connection = new AdomdConnection(connectionString);
        }

        /// <summary>
        /// Opens a connection if the current connection is closed or broken.
        /// </summary>
        void openConnection()
        {
            if (this._connection.State == ConnectionState.Closed)
            {
                if (this._connection == null)
                    this._connection = new AdomdConnection(CubeBase.ConnectionString);
                this._connection.Open();
            }

            else if (this._connection.State == ConnectionState.Broken)
            {
                this._connection.Close();
                this._connection.Open();
            }

            else return;
        }

        public CellSet GetCellSet(string mdx)
        {
            using(var command = this.prepareCommand(mdx))
                return command.ExecuteCellSet();
        }

        public AdomdDataReader GetReader(string mdx)
        {
            using (var command = this.prepareCommand(mdx))
                return command.ExecuteReader();
        }

        public DataTable GetDataTable(string mdx)
        {
            using (var command = this.prepareCommand(mdx))
            {
                var table = new DataTable("CubeResults");
                using(var daptor = new AdomdDataAdapter(command))
                {
                    daptor.Fill(table);
                }
                return table;
            }
        }

        AdomdCommand prepareCommand(string mdx)
        {
            this.openConnection();
            var command = new AdomdCommand(mdx, this._connection);
            return command;
        }

        public void Dispose()
        {
            this._connection.Dispose();
        }
    }
}
