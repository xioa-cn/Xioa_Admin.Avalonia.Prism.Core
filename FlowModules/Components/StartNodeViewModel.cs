using System.Collections.ObjectModel;
using Avalonia;
using NodifyM.Avalonia.ViewModelBase;

namespace FlowModules.Components;

public class StartNodeViewModel : NodeViewModelBase
{
    public StartNodeViewModel(string title,Point position)
    {
        this.Location = position;
        this.Title = title;
        Initialized();
    }

    private void Initialized()
    {
        Output = new ObservableCollection<object>
        {
            new ConnectorViewModelBase()
            {
                Title = "Output 2",
                Flow = ConnectorViewModelBase.ConnectorFlow.Output
            }
        };
    }
    
}