using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace XmlComparer.Core
{
    /// <summary>
    /// Applies XML patches to XML documents.
    /// </summary>
    /// <remarks>
    /// <para>The XmlPatchApplier takes an <see cref="XmlPatch"/> and applies its operations
    /// to an XML document, transforming the original document into the patched version.</para>
    /// <para>Features:</para>
    /// <list type="bullet">
    ///   <item><description>Applies operations in order</description></item>
    ///   <item><description>Supports conditional application based on conditions</description></item>
    ///   <item><description>Validates old values before applying replacements</description></item>
    ///   <item><description>Detailed error reporting for failed operations</description></item>
    ///   <item><option>Rollback support on failure</item></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var applier = new XmlPatchApplier();
    ///
    /// // Apply to a string
    /// var result = applier.ApplyToString(patch, originalXml);
    ///
    /// // Apply to an XDocument
    /// var result = applier.Apply(patch, document);
    ///
    /// // Apply with rollback on failure
    /// var result = applier.Apply(patch, document, new PatchApplierOptions
    /// {
    ///     StopOnError = true,
    ///     CreateRollbackInfo = true
    /// });
    /// </code>
    /// </example>
    /// <seealso cref="XmlPatch"/>
    /// <seealso cref="XmlPatchGenerator"/>
    public class XmlPatchApplier
    {
        /// <summary>
        /// Gets or sets the options for patch application.
        /// </summary>
        public PatchApplierOptions Options { get; set; } = new PatchApplierOptions();

        /// <summary>
        /// Creates a new XmlPatchApplier with default options.
        /// </summary>
        public XmlPatchApplier() { }

        /// <summary>
        /// Creates a new XmlPatchApplier with the specified options.
        /// </summary>
        /// <param name="options">The applier options.</param>
        public XmlPatchApplier(PatchApplierOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Applies a patch to an XML string.
        /// </summary>
        /// <param name="patch">The patch to apply.</param>
        /// <param name="xml">The original XML string.</param>
        /// <returns>A patch application result.</returns>
        public PatchApplicationResult ApplyToString(XmlPatch patch, string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                return Apply(patch, doc);
            }
            catch (Exception ex)
            {
                return PatchApplicationResult.Failure("Failed to parse XML", ex);
            }
        }

        /// <summary>
        /// Applies a patch to an XDocument.
        /// </summary>
        /// <param name="patch">The patch to apply.</param>
        /// <param name="document">The document to patch.</param>
        /// <returns>A patch application result.</returns>
        public PatchApplicationResult Apply(XmlPatch patch, XDocument document)
        {
            var result = new PatchApplicationResult
            {
                Success = true,
                AppliedOperations = new List<PatchOperationResult>(),
                FailedOperations = new List<PatchOperationResult>()
            };

            // Create a copy for rollback if needed
            XDocument? rollbackDoc = Options.CreateRollbackInfo ? new XDocument(document) : null;

            try
            {
                foreach (var operation in patch.Operations)
                {
                    var opResult = ApplyOperation(operation, document);

                    if (opResult.Success)
                    {
                        result.AppliedOperations.Add(opResult);
                    }
                    else
                    {
                        result.FailedOperations.Add(opResult);
                        result.Success = false;

                        if (Options.StopOnError)
                        {
                            result.ErrorMessage = $"Patch application stopped at operation: {opResult.ErrorMessage}";

                            // Rollback if configured
                            if (Options.CreateRollbackInfo && rollbackDoc != null)
                            {
                                result.RollbackDocument = rollbackDoc;
                            }

                            break;
                        }
                    }
                }

                if (result.Success)
                {
                    result.PatchedDocument = document;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Patch application failed: {ex.Message}";
                result.Exception = ex;

                // Rollback if configured
                if (Options.CreateRollbackInfo && rollbackDoc != null)
                {
                    result.RollbackDocument = rollbackDoc;
                }

                return result;
            }
        }

        /// <summary>
        /// Applies a single patch operation to a document.
        /// </summary>
        private PatchOperationResult ApplyOperation(XmlPatchOperation operation, XDocument document)
        {
            var result = new PatchOperationResult
            {
                Operation = operation
            };

            try
            {
                // Evaluate condition if present
                if (!string.IsNullOrEmpty(operation.Condition))
                {
                    bool conditionMet = EvaluateCondition(operation.Condition, document);
                    if (!conditionMet)
                    {
                        result.Success = true;
                        result.Skipped = true;
                        result.Message = "Condition not met, operation skipped";
                        return result;
                    }
                }

                switch (operation.Type)
                {
                    case PatchOperationType.Add:
                        return ApplyAddOperation(operation, document);

                    case PatchOperationType.Remove:
                        return ApplyRemoveOperation(operation, document);

                    case PatchOperationType.Replace:
                        return ApplyReplaceOperation(operation, document);

                    case PatchOperationType.Move:
                        return ApplyMoveOperation(operation, document);

                    case PatchOperationType.ChangeNamespace:
                        return ApplyNamespaceChangeOperation(operation, document);

                    default:
                        return PatchOperationResult.Failed(operation, $"Unknown operation type: {operation.Type}");
                }
            }
            catch (Exception ex)
            {
                return PatchOperationResult.Failed(operation, $"Operation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies an Add operation.
        /// </summary>
        private PatchOperationResult ApplyAddOperation(XmlPatchOperation operation, XDocument document)
        {
            if (string.IsNullOrEmpty(operation.Content))
            {
                return PatchOperationResult.Failed(operation, "Add operation has no content");
            }

            var target = SelectTarget(document, operation.TargetPath);
            if (target == null)
            {
                return PatchOperationResult.Failed(operation, $"Target not found: {operation.TargetPath}");
            }

            if (target is not XElement targetElement)
            {
                return PatchOperationResult.Failed(operation, $"Target must be an element, not a {target.GetType().Name}");
            }

            var newElement = XElement.Parse(operation.Content);

            switch (operation.Position)
            {
                case PatchPosition.Start:
                    targetElement.AddFirst(newElement);
                    break;

                case PatchPosition.End:
                    targetElement.Add(newElement);
                    break;

                case PatchPosition.Before:
                    if (targetElement.Parent == null)
                        return PatchOperationResult.Failed(operation, "Cannot add before root element");
                    targetElement.AddBeforeSelf(newElement);
                    break;

                case PatchPosition.After:
                    if (targetElement.Parent == null)
                        return PatchOperationResult.Failed(operation, "Cannot add after root element");
                    targetElement.AddAfterSelf(newElement);
                    break;

                case PatchPosition.Replace:
                    targetElement.ReplaceWith(newElement);
                    break;
            }

            return PatchOperationResult.Succeeded(operation);
        }

        /// <summary>
        /// Applies a Remove operation.
        /// </summary>
        private PatchOperationResult ApplyRemoveOperation(XmlPatchOperation operation, XDocument document)
        {
            var target = SelectTarget(document, operation.TargetPath);
            if (target == null)
            {
                return PatchOperationResult.Failed(operation, $"Target not found: {operation.TargetPath}");
            }

            // Verify old value if present
            if (!string.IsNullOrEmpty(operation.OldValue))
            {
                string currentValue = target.ToString();
                if (currentValue != operation.OldValue)
                {
                    return PatchOperationResult.Failed(operation,
                        $"Old value mismatch. Expected: {operation.OldValue}, Got: {currentValue}");
                }
            }

            // Handle removal based on type
            if (target is XElement elem)
            {
                elem.Remove();
            }
            else if (target is XAttribute attr)
            {
                attr.Remove();
            }
            else
            {
                return PatchOperationResult.Failed(operation, $"Cannot remove {target.GetType().Name}");
            }

            return PatchOperationResult.Succeeded(operation);
        }

        /// <summary>
        /// Applies a Replace operation.
        /// </summary>
        private PatchOperationResult ApplyReplaceOperation(XmlPatchOperation operation, XDocument document)
        {
            var target = SelectTarget(document, operation.TargetPath);
            if (target == null)
            {
                return PatchOperationResult.Failed(operation, $"Target not found: {operation.TargetPath}");
            }

            // Verify old value if present
            if (!string.IsNullOrEmpty(operation.OldValue))
            {
                string currentValue = target.ToString();
                if (currentValue != operation.OldValue)
                {
                    return PatchOperationResult.Failed(operation,
                        $"Old value mismatch. Expected: {operation.OldValue}, Got: {currentValue}");
                }
            }

            // Replace with content or value
            if (!string.IsNullOrEmpty(operation.Content))
            {
                if (target is not XElement targetElement)
                {
                    return PatchOperationResult.Failed(operation, "Replace with content requires an element target");
                }
                var newElement = XElement.Parse(operation.Content);
                targetElement.ReplaceWith(newElement);
            }
            else if (!string.IsNullOrEmpty(operation.NewValue))
            {
                // Replace just the value
                if (target is XElement elem)
                {
                    elem.Value = operation.NewValue;
                }
                else if (target is XAttribute attr)
                {
                    attr.Value = operation.NewValue;
                }
            }

            return PatchOperationResult.Succeeded(operation);
        }

        /// <summary>
        /// Applies a Move operation.
        /// </summary>
        private PatchOperationResult ApplyMoveOperation(XmlPatchOperation operation, XDocument document)
        {
            var source = SelectTarget(document, operation.TargetPath);
            if (source == null)
            {
                return PatchOperationResult.Failed(operation, $"Source not found: {operation.TargetPath}");
            }

            if (source is not XElement sourceElement)
            {
                return PatchOperationResult.Failed(operation, "Source must be an element");
            }

            var destinationPath = operation.NewValue;
            if (string.IsNullOrEmpty(destinationPath))
            {
                return PatchOperationResult.Failed(operation, "Move operation has no destination");
            }

            var destination = SelectTarget(document, destinationPath);
            if (destination == null)
            {
                return PatchOperationResult.Failed(operation, $"Destination not found: {destinationPath}");
            }

            if (destination is not XElement destinationElement)
            {
                return PatchOperationResult.Failed(operation, "Destination must be an element");
            }

            // Clone and remove from source
            var cloned = new XElement(sourceElement);
            sourceElement.Remove();

            // Add to destination
            destinationElement.Add(cloned);

            return PatchOperationResult.Succeeded(operation);
        }

        /// <summary>
        /// Applies a namespace change operation.
        /// </summary>
        private PatchOperationResult ApplyNamespaceChangeOperation(XmlPatchOperation operation, XDocument document)
        {
            // Namespace changes are treated as replace operations
            return ApplyReplaceOperation(operation, document);
        }

        /// <summary>
        /// Selects a target element or attribute using XPath.
        /// </summary>
        private XObject? SelectTarget(XDocument document, string xpath)
        {
            try
            {
                // Try to select an element first
                var element = document.XPathSelectElement(xpath);
                if (element != null) return element;

                // Try to select an attribute
                var attribute = document.XPathSelectElement(xpath.Substring(0, xpath.LastIndexOf('/')))
                    ?.Attribute(xpath.Substring(xpath.LastIndexOf('@') + 1));
                if (attribute != null) return attribute;

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Evaluates a condition XPath expression.
        /// </summary>
        private bool EvaluateCondition(string condition, XDocument document)
        {
            try
            {
                var result = document.XPathEvaluate(condition);
                if (result is bool boolResult)
                {
                    return boolResult;
                }
                return true;
            }
            catch
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Result of a patch application operation.
    /// </summary>
    public class PatchApplicationResult
    {
        /// <summary>
        /// Gets or sets whether the patch was fully applied.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the patched document.
        /// </summary>
        public XDocument? PatchedDocument { get; set; }

        /// <summary>
        /// Gets or sets the rollback document (if rollback was enabled).
        /// </summary>
        public XDocument? RollbackDocument { get; set; }

        /// <summary>
        /// Gets or sets the successfully applied operations.
        /// </summary>
        public List<PatchOperationResult> AppliedOperations { get; set; } = new List<PatchOperationResult>();

        /// <summary>
        /// Gets or sets the failed operations.
        /// </summary>
        public List<PatchOperationResult> FailedOperations { get; set; } = new List<PatchOperationResult>();

        /// <summary>
        /// Gets or sets an error message if the patch failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the exception if the patch failed.
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets whether any operations were skipped.
        /// </summary>
        public bool HasSkippedOperations => AppliedOperations.Any(o => o.Skipped);

        /// <summary>
        /// Gets the patched XML as a string.
        /// </summary>
        public string? GetPatchedXml()
        {
            return PatchedDocument?.ToString();
        }

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        public static PatchApplicationResult Failure(string message, Exception? exception = null)
        {
            return new PatchApplicationResult
            {
                Success = false,
                ErrorMessage = message,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Result of a single patch operation.
    /// </summary>
    public class PatchOperationResult
    {
        /// <summary>
        /// Gets or sets the operation that was applied.
        /// </summary>
        public XmlPatchOperation? Operation { get; set; }

        /// <summary>
        /// Gets or sets whether the operation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets whether the operation was skipped.
        /// </summary>
        public bool Skipped { get; set; }

        /// <summary>
        /// Gets or sets a message describing the result.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets an error message if the operation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Creates a success result.
        /// </summary>
        public static PatchOperationResult Succeeded(XmlPatchOperation operation)
        {
            return new PatchOperationResult
            {
                Operation = operation,
                Success = true
            };
        }

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        public static PatchOperationResult Failed(XmlPatchOperation operation, string errorMessage)
        {
            return new PatchOperationResult
            {
                Operation = operation,
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Options for patch application.
    /// </summary>
    public class PatchApplierOptions
    {
        /// <summary>
        /// Gets or sets whether to stop applying operations on first error.
        /// </summary>
        /// <remarks>
        /// When true, patch application stops at the first failed operation.
        /// When false, all operations are attempted and failures are collected.
        /// Default is true.
        /// </remarks>
        public bool StopOnError { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to create rollback information.
        /// </summary>
        /// <remarks>
        /// When true, a copy of the original document is kept for rollback
        /// if the patch fails. Default is false.
        /// </remarks>
        public bool CreateRollbackInfo { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to validate old values before applying operations.
        /// </summary>
        /// <remarks>
        /// When true, operations with OldValue will only apply if the current
        /// value matches. Default is true.
        /// </remarks>
        public bool ValidateOldValues { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to evaluate conditions.
        /// </summary>
        /// <remarks>
        /// When true, operations with conditions will only apply if the
        /// condition evaluates to true. Default is true.
        /// </remarks>
        public bool EvaluateConditions { get; set; } = true;
    }
}
