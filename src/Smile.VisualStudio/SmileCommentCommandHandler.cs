using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Smile.Language;

namespace Smile.VisualStudio;

[Export(typeof(ICommandHandler))]
[Name(nameof(SmileCommentCommandHandler))]
[Order(Before = "default")]
[ContentType(SmileContentType.Name)]
[TextViewRole(PredefinedTextViewRoles.Editable)]
internal sealed class SmileCommentCommandHandler :
    IChainedCommandHandler<CommentSelectionCommandArgs>,
    IChainedCommandHandler<UncommentSelectionCommandArgs>,
    IChainedCommandHandler<ToggleLineCommentCommandArgs>
{
    public string DisplayName => "SMILE Comment Selection";

    public CommandState GetCommandState(CommentSelectionCommandArgs args,
        Func<CommandState> nextCommandHandler) => CommandState.Available;

    public CommandState GetCommandState(UncommentSelectionCommandArgs args,
        Func<CommandState> nextCommandHandler) => CommandState.Available;

    public CommandState GetCommandState(ToggleLineCommentCommandArgs args,
        Func<CommandState> nextCommandHandler) => CommandState.Available;

    public void ExecuteCommand(CommentSelectionCommandArgs args, Action nextCommandHandler,
        CommandExecutionContext executionContext) =>
        Execute(args.TextView, args.SubjectBuffer, SmileCommentMode.Comment, nextCommandHandler);

    public void ExecuteCommand(UncommentSelectionCommandArgs args, Action nextCommandHandler,
        CommandExecutionContext executionContext) =>
        Execute(args.TextView, args.SubjectBuffer, SmileCommentMode.Uncomment, nextCommandHandler);

    public void ExecuteCommand(ToggleLineCommentCommandArgs args, Action nextCommandHandler,
        CommandExecutionContext executionContext) =>
        Execute(args.TextView, args.SubjectBuffer, SmileCommentMode.Toggle, nextCommandHandler);

    private static void Execute(ITextView textView, ITextBuffer subjectBuffer, SmileCommentMode mode,
        Action nextCommandHandler)
    {
        try
        {
            var selection = textView.Selection.StreamSelectionSpan.SnapshotSpan;
            if (selection.Snapshot.TextBuffer != subjectBuffer)
            {
                nextCommandHandler();
                return;
            }

            var snapshot = subjectBuffer.CurrentSnapshot;
            var edits = SmileCommentService.GetEdits(snapshot.GetText(), selection.Start.Position,
                selection.Length, mode);
            if (edits.Count == 0)
                return;

            var wasEmpty = selection.IsEmpty;
            var wasReversed = textView.Selection.IsReversed;
            var startTrackingPoint = snapshot.CreateTrackingPoint(selection.Start.Position,
                PointTrackingMode.Negative);
            var endTrackingPoint = snapshot.CreateTrackingPoint(selection.End.Position,
                PointTrackingMode.Positive);
            var caretTrackingPoint = snapshot.CreateTrackingPoint(textView.Caret.Position.BufferPosition.Position,
                PointTrackingMode.Positive);

            using (var textEdit = subjectBuffer.CreateEdit())
            {
                foreach (var edit in edits)
                {
                    if (!textEdit.Replace(edit.Position, edit.DeleteLength, edit.InsertText))
                    {
                        textEdit.Cancel();
                        nextCommandHandler();
                        return;
                    }
                }

                snapshot = textEdit.Apply();
            }

            if (wasEmpty)
            {
                textView.Selection.Clear();
                textView.Caret.MoveTo(caretTrackingPoint.GetPoint(snapshot));
            }
            else
            {
                var start = startTrackingPoint.GetPoint(snapshot);
                var end = endTrackingPoint.GetPoint(snapshot);
                textView.Selection.Select(new SnapshotSpan(start, end), wasReversed);
            }
        }
        catch (Exception exception)
        {
            ActivityLog.LogError(nameof(SmileCommentCommandHandler), exception.ToString());
            nextCommandHandler();
        }
    }
}
