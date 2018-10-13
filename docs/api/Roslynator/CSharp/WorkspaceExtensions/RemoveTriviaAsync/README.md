# WorkspaceExtensions\.RemoveTriviaAsync\(Document, TextSpan, CancellationToken\) Method

[Home](../../../../README.md)

**Containing Type**: Roslynator\.CSharp\.[WorkspaceExtensions](../README.md)

**Assembly**: Roslynator\.CSharp\.Workspaces\.dll

## Summary

Creates a new document with trivia inside the specified span removed\.

```csharp
public static Task<Document> RemoveTriviaAsync(this Document document, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
```

### Parameters

**document**

**span**

**cancellationToken**

### Returns

System\.Threading\.Tasks\.[Task](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1)\<[Document](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.document)>
