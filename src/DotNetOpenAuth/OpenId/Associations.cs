﻿//-----------------------------------------------------------------------
// <copyright file="Associations.cs" company="Andrew Arnott">
//     Copyright (c) Andrew Arnott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.OpenId {
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.Linq;
	using DotNetOpenAuth.Messaging;

	/// <summary>
	/// A dictionary of handle/Association pairs.
	/// </summary>
	/// <remarks>
	/// Each method is locked, even if it is only one line, so that they are thread safe
	/// against each other, particularly the ones that enumerate over the list, since they
	/// can break if the collection is changed by another thread during enumeration.
	/// </remarks>
	[DebuggerDisplay("Count = {assocs.Count}")]
	internal class Associations {
		/// <summary>
		/// The lookup table where keys are the association handles and values are the associations themselves.
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private readonly KeyedCollection<string, Association> associations = new KeyedCollectionDelegate<string, Association>(assoc => assoc.Handle);

		/// <summary>
		/// Initializes a new instance of the <see cref="Associations"/> class.
		/// </summary>
		public Associations() {
		}

		/// <summary>
		/// Gets the <see cref="Association"/>s ordered in order of descending issue date
		/// (most recently issued comes first).  An empty sequence if no valid associations exist.
		/// </summary>
		/// <remarks>
		/// This property is used by relying parties that are initiating authentication requests.
		/// It does not apply to Providers, which always need a specific association by handle.
		/// </remarks>
		public IEnumerable<Association> Best {
			get {
				lock (this.associations) {
					return this.associations.OrderByDescending(assoc => assoc.Issued);
				}
			}
		}

		/// <summary>
		/// Stores an <see cref="Association"/> in the collection.
		/// </summary>
		/// <param name="association">The association to add to the collection.</param>
		public void Set(Association association) {
			ErrorUtilities.VerifyArgumentNotNull(association, "association");
			lock (this.associations) {
				this.associations.Remove(association.Handle); // just in case one already exists.
				this.associations.Add(association);
			}
		}

		/// <summary>
		/// Returns the <see cref="Association"/> with the given handle.  Null if not found.
		/// </summary>
		/// <param name="handle">The handle to the required association.</param>
		/// <returns>The desired association, or null if none with the given handle could be found.</returns>
		public Association Get(string handle) {
			lock (this.associations) {
				if (this.associations.Contains(handle)) {
					return this.associations[handle];
				} else {
					return null;
				}
			}
		}

		/// <summary>
		/// Removes the <see cref="Association"/> with the given handle.
		/// </summary>
		/// <param name="handle">The handle to the required association.</param>
		/// <returns>Whether an <see cref="Association"/> with the given handle was in the collection for removal.</returns>
		public bool Remove(string handle) {
			lock (this.associations) {
				return this.associations.Remove(handle);
			}
		}

		/// <summary>
		/// Removes all expired associations from the collection.
		/// </summary>
		public void ClearExpired() {
			lock (this.associations) {
				var expireds = this.associations.Where(assoc => assoc.IsExpired).ToList();
				foreach (Association assoc in expireds) {
					this.associations.Remove(assoc.Handle);
				}
			}
		}
	}
}
