﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using DanTup.DartAnalysis.Json;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace DanTup.DartVS
{
	[Export(typeof(ICompletionSourceProvider))]
	[ContentType(DartContentTypeDefinition.DartContentType)]
	[Name("Dart Completion")]
	class CompletionSourceProvider : ICompletionSourceProvider
	{
		[Import]
		ITextDocumentFactoryService textDocumentFactory = null;

		[Import]
		DartAnalysisService analysisService = null;

		public ICompletionSource TryCreateCompletionSource(ITextBuffer buffer)
		{
			return new CompletionSource(this, buffer, textDocumentFactory, analysisService);
		}
	}

	class CompletionSource : ICompletionSource
	{
		CompletionSourceProvider provider;
		ITextBuffer buffer;
		ITextDocumentFactoryService textDocumentFactory;
		DartAnalysisService analysisService;

		public CompletionSource(CompletionSourceProvider provider, ITextBuffer buffer, ITextDocumentFactoryService textDocumentFactory, DartAnalysisService analysisService)
		{
			this.provider = provider;
			this.buffer = buffer;
			this.textDocumentFactory = textDocumentFactory;
			this.analysisService = analysisService;
		}

		public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
		{
			var triggerPoint = session.GetTriggerPoint(buffer.CurrentSnapshot);
			if (triggerPoint == null)
				return;

			ITextDocument doc;
			if (!textDocumentFactory.TryGetTextDocument(buffer, out doc))
				return;

			var applicableTo = buffer.CurrentSnapshot.CreateTrackingSpan(new SnapshotSpan(triggerPoint.Value, 1), SpanTrackingMode.EdgeInclusive);

			var completions = new ObservableCollection<Completion>();
			completions.Add(new Completion("Hard-coded..."));

			var completionSet = new CompletionSet("All", "All", applicableTo, Enumerable.Empty<Completion>(), completions);
			completionSets.Add(completionSet);

			Task.Run(async () =>
			{
				await Task.Delay(1000);
				completions.Add(new Completion("Danny"));
				session.Start();
			});

			//// Kick of async request to update the results.
			//analysisService
			//	.GetSuggestions(doc.FilePath, triggerPoint.Value.Position)
			//	.ContinueWith(completion =>
			//	{
			//		var completionID = completion.Result;
			//		IDisposable subscription = null;
			//		subscription = analysisService
			//			.CompletionResultsNotification
			//			.Where(c => c.Id == completionID)
			//			.Subscribe(c => UpdateCompletionResults(subscription, completions, c));
			//	});
		}

		void UpdateCompletionResults(IDisposable subscription, ObservableCollection<Completion> completions, CompletionResultsNotification results)
		{
			foreach (var result in results.Results)
			{
				completions.Add(new Completion(result.Completion)); //, result.Completion, result.DocComplete, null, null));
			}

			// If we were notified that this was the last set of results; remove the subscription.
			if (results.Last)
				subscription.Dispose();
		}

		public void Dispose()
		{
			GC.SuppressFinalize(true);
		}
	}
}
