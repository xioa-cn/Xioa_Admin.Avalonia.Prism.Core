using System;
using Ava.Xioa.Common.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Input;
using FlowModules.Components;
using NodifyM.Avalonia.ViewModelBase;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlowModules.ViewModels;

[PrismViewModel(typeof(FlowViewModel), ServiceLifetime.Singleton)]
public partial class FlowViewModel : NodifyEditorViewModelBase
{
    public FlowViewModel()
    {
        var knot1 = new KnotNodeViewModel()
        {
            Location = new Point(300, 100)
        };
        var input1 = new ConnectorViewModelBase()
        {
            Title = "AS 1",
            Flow = ConnectorViewModelBase.ConnectorFlow.Input
        };
        var output1 = new ConnectorViewModelBase()
        {
            Title = "B 1",
            Flow = ConnectorViewModelBase.ConnectorFlow.Output
        };
        Connections.Add(new ConnectionViewModelBase(this, output1, knot1.Connector, "Test"));
        Connections.Add(new ConnectionViewModelBase(this, knot1.Connector, input1));
        Nodes = new()
        {
            new NodeViewModelBase()
            {
                Location = new Point(400, 200),
                Title = "Node 1",
                Input = new ObservableCollection<object>
                {
                    input1,
                    new ComboBox()
                    {
                        ItemsSource = new ObservableCollection<object>
                        {
                            "Item 1",
                            "Item 2",
                            "Item 3"
                        }
                    }
                },
                Output = new ObservableCollection<object>
                {
                    new ConnectorViewModelBase()
                    {
                        Title = "Output 2",
                        Flow = ConnectorViewModelBase.ConnectorFlow.Output
                    }
                }
            },
            new NodeViewModelBase()
            {
                Title = "Node 2",
                Location = new Point(-100, -100),
                Input = new ObservableCollection<object>
                {
                    new ConnectorViewModelBase()
                    {
                        Title = "Input 1",
                        Flow = ConnectorViewModelBase.ConnectorFlow.Input
                    },
                    new ConnectorViewModelBase()
                    {
                        Flow = ConnectorViewModelBase.ConnectorFlow.Input,
                        Title = "Input 2"
                    }
                },
                Output = new ObservableCollection<object>
                {
                    output1,
                    new ConnectorViewModelBase()
                    {
                        Flow = ConnectorViewModelBase.ConnectorFlow.Output,
                        Title = "Output 1"
                    },
                    new ConnectorViewModelBase()
                    {
                        Flow = ConnectorViewModelBase.ConnectorFlow.Output,
                        Title = "Output 2"
                    }
                }
            }
        };
        Nodes.Add(knot1);
        knot1.Connector.IsConnected = true;
        output1.IsConnected = true;
        input1.IsConnected = true;

        // Zoom = 1;
        // OffsetX = 0;
        // OffsetY = 0;
    }

    public override void Connect(ConnectorViewModelBase source, ConnectorViewModelBase target)
    {
        base.Connect(source, target);
    }

    public override void DisconnectConnector(ConnectorViewModelBase connector)
    {
        base.DisconnectConnector(connector);
    }
    

    [RelayCommand]
    private void DeleteNode(object? nodeObj)
    {
        if (nodeObj is KnotNodeViewModel knot)
        {
            var toRemove = Connections.Where(c => Equals(c.Source, knot.Connector) || Equals(c.Target, knot.Connector))
                .ToList();
            foreach (var c in toRemove)
            {
                Connections.Remove(c);
            }

            knot.Connector.IsConnected = false;
            Nodes.Remove(knot);
            return;
        }

        if (nodeObj is NodeViewModelBase node)
        {
            var connectors = new List<ConnectorViewModelBase>();
            foreach (var i in node.Input)
            {
                if (i is ConnectorViewModelBase ci) connectors.Add(ci);
            }

            foreach (var o in node.Output)
            {
                if (o is ConnectorViewModelBase co) connectors.Add(co);
            }

            var toRemove = Connections.Where(c => connectors.Contains(c.Source) || connectors.Contains(c.Target))
                .ToList();
            foreach (var c in toRemove)
            {
                Connections.Remove(c);
            }

            foreach (var conn in connectors)
            {
                conn.IsConnected = false;
            }

            Nodes.Remove(node);
        }
    }
    
    [RelayCommand]
    private void AddNode()
    {
        Nodes.Add(
            (new InOutputNodeViewModel("测试节点", new Point(100, 100)))
            .AddInput("测试输入")
            .AddOutput("测试输出")
        );
    }
    
    [ObservableProperty] private double _zoom = 1;
    
    // [RelayCommand]
    // private void ResetView()
    // {
    //     Dispatcher.UIThread.Post(() =>
    //     {
    //         Zoom = 1;
    //         OffsetX = 0;
    //         OffsetY = 0;
    //     }, DispatcherPriority.Render);
    // }

    // [RelayCommand]
    // private void ZoomToFit(object? viewportObj)
    // {
    //     double viewW = 0, viewH = 0;
    //     if (viewportObj is Avalonia.Rect rect)
    //     {
    //         viewW = rect.Width;
    //         viewH = rect.Height;
    //     }
    //
    //     if (viewW <= 0 || viewH <= 0)
    //     {
    //         return;
    //     }
    //
    //     var points = new List<Point>();
    //     foreach (var n in Nodes)
    //     {
    //         if (n is KnotNodeViewModel kn)
    //         {
    //             points.Add(kn.Location);
    //             if (kn.Connector.Anchor != default)
    //             {
    //                 points.Add(kn.Connector.Anchor);
    //             }
    //         }
    //         else if (n is NodeViewModelBase node)
    //         {
    //             points.Add(node.Location);
    //             foreach (var i in node.Input)
    //             {
    //                 if (i is ConnectorViewModelBase ci && ci.Anchor != default)
    //                 {
    //                     points.Add(ci.Anchor);
    //                 }
    //             }
    //
    //             foreach (var o in node.Output)
    //             {
    //                 if (o is ConnectorViewModelBase co && co.Anchor != default)
    //                 {
    //                     points.Add(co.Anchor);
    //                 }
    //             }
    //         }
    //     }
    //
    //     double minX, minY, maxX, maxY;
    //     if (points.Count > 0)
    //     {
    //         minX = points.Min(p => p.X);
    //         minY = points.Min(p => p.Y);
    //         maxX = points.Max(p => p.X);
    //         maxY = points.Max(p => p.Y);
    //     }
    //     else
    //     {
    //         var locs = Nodes.Select(o =>
    //         {
    //             if (o is KnotNodeViewModel kn) return kn.Location;
    //             if (o is NodeViewModelBase nd) return nd.Location;
    //             return default(Point);
    //         }).Where(p => p != default).ToList();
    //         if (locs.Count == 0)
    //         {
    //             ResetView();
    //             return;
    //         }
    //
    //         minX = locs.Min(p => p.X);
    //         minY = locs.Min(p => p.Y);
    //         maxX = locs.Max(p => p.X);
    //         maxY = locs.Max(p => p.Y);
    //     }
    //
    //     const double pad = 120;
    //     var contentW = Math.Max(1, (maxX - minX)) + pad * 2;
    //     var contentH = Math.Max(1, (maxY - minY)) + pad * 2;
    //
    //     var zx = viewW / contentW;
    //     var zy = viewH / contentH;
    //     var newZoom = Math.Min(zx, zy);
    //     newZoom = Math.Clamp(newZoom * 0.95, 0.1, 10);
    //
    //     var worldCenterX = minX + (maxX - minX) / 2;
    //     var worldCenterY = minY + (maxY - minY) / 2;
    //     var viewportWorldHalfW = (viewW / newZoom) / 2;
    //     var viewportWorldHalfH = (viewH / newZoom) / 2;
    //
    //     var newOffsetX = -(worldCenterX - viewportWorldHalfW);
    //     var newOffsetY = -(worldCenterY - viewportWorldHalfH);
    //
    //     Dispatcher.UIThread.Post(() =>
    //     {
    //         Zoom = newZoom;
    //         OffsetX = newOffsetX;
    //         OffsetY = newOffsetY;
    //     }, DispatcherPriority.Render);
    // }
}