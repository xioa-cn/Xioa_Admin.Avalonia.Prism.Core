using System.Collections.ObjectModel;
using Avalonia;
using NodifyM.Avalonia.ViewModelBase;

namespace FlowModules.Components;

public class InOutputNodeViewModel : NodeViewModelBase
{
    public InOutputNodeViewModel(string title, Point position)
    {
        this.Location = position;
        this.Title = title;
        Initialized();
    }

    private void Initialized()
    {
        Output = new ObservableCollection<object>();
        Input = new ObservableCollection<object>();
    }

    public InOutputNodeViewModel AddInput(object input)
    {
        Input.Add(input);
        return this;
    }

    public InOutputNodeViewModel AddInput(string input)
    {
        Input.Add(new ConnectorViewModelBase()
        {
            Title = input,
            Flow = ConnectorViewModelBase.ConnectorFlow.Input
        });
        return this;
    }

    public InOutputNodeViewModel AddOutput(object output)
    {
        Output.Add(output);
        return this;
    }

    public InOutputNodeViewModel AddOutput(string output)
    {
        Output.Add(new ConnectorViewModelBase()
        {
            Title = output,
            Flow = ConnectorViewModelBase.ConnectorFlow.Output
        });
        return this;
    }
}