using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Carto;

namespace ConstructwithBuffer_Addin
{
    public class FreehandBufferedLineConstruction : ESRI.ArcGIS.Desktop.AddIns.Tool
    {
        public FreehandBufferedLineConstruction()
        {    
          UID editorUid = new UID();
          editorUid.Value = "esriEditor.Editor";
          m_editor = ArcMap.Application.FindExtensionByCLSID(editorUid) as IEditor3;          
        }

    protected override void OnActivate()
    {      
      //get the snapping environment
      m_SnappingEnvironment = GetSnappingEnvironment();

      base.OnActivate();
    }

    protected sealed override void OnMouseDown(MouseEventArgs arg)
    {
      IPoint point = m_editor.Display.DisplayTransformation.ToMapPoint(arg.X, arg.Y);

      if (m_LineFeedback == null)
      {
        //first mouse click - start feedback
        m_LineFeedback = new NewLineFeedbackClass();
        m_LineFeedback.Display = m_editor.Display;
        m_LineFeedback.Start(point);
      }
      else
      {
        //second mouse click - end feedback and create feature
        IPolyline polyline = m_LineFeedback.Stop();
        m_LineFeedback = null;
                
        IEditSketch editSketch = m_editor as IEditSketch;

        editSketch.Geometry = this.Buffer((IGeometry)polyline, BufferDistance);

        ICommandBars documentBars = ArcMap.Application.Document.CommandBars;
        Guid cmdID = new System.Guid("{5d0dfa35-8af0-4db1-965d-e4f3579f0cb8}");
        //cmdID.Value = "{09C6F589-A3CE-48AB-81BC-46965C58F264}";
        ICommandItem cmdItem = documentBars.Find(cmdID, false, false);
        
        IEditTemplate CurEditTemplate = m_editor.CurrentTemplate;

        //constructwithbuffer
        System.Guid cwb = CurEditTemplate.Tool;

        //circle
        CurEditTemplate.Tool = cmdID;        

        //switch back
        CurEditTemplate.Tool = cwb;

        //IEditTask task = m_editor.CurrentTask;

        //IEditTask edittask = m_editor.CurrentTask as IEditTask;
        //IEditTaskSearch etsearch = edittask as IEditTaskSearch;
        //edittask = etsearch.get_TaskByUniqueName("GarciaUI_ModifyFeatureTask");
        //m_editor.CurrentTask = edittask;

        //m_editor.CurrentTask = task;
        
        //IEditTemplate firsttemplate = m_editor.CurrentTemplate;
        //m_editor.CurrentTemplate = null;
        //m_editor.CurrentTemplate = firsttemplate;
        //m_editor.CurrentTemplate = m_editor.Template[0];
        
        Console.WriteLine(m_editor.CurrentTemplate.Name);
        editSketch.FinishSketch();
      }
      base.OnMouseDown(arg);
    }

    protected sealed override void OnMouseMove(MouseEventArgs arg)
    {
      m_CurrentMouseCoords = QueryMapPoint(m_editor.Display, arg.X, arg.Y);

      ISnappingResult snapResult = m_SnappingEnvironment.PointSnapper.Snap(m_CurrentMouseCoords);

      if (m_SnappingFeedback == null)
      {
        m_SnappingFeedback = new SnappingFeedbackClass();
        m_SnappingFeedback.Initialize(ArcMap.Application, m_SnappingEnvironment, true);
      }
      m_SnappingFeedback.Update(snapResult, 0);

      if (snapResult != null)
        m_CurrentMouseCoords = snapResult.Location;

      if (m_LineFeedback != null)
      {
        IPoint point = m_editor.Display.DisplayTransformation.ToMapPoint(arg.X, arg.Y);
        m_LineFeedback.AddPoint(point);
      }
      base.OnMouseMove(arg);
    }

    protected override void OnUpdate()
    {
      Enabled = ArcMap.Application != null;
    }
    protected override void OnRefresh(int hDC)
    {
      if (m_SnappingFeedback != null)
        m_SnappingFeedback.Refresh(hDC);

      base.OnRefresh(hDC);
    }
    protected override void OnKeyDown(KeyEventArgs arg)
    {
      //cancel if escape is pressed
      if (arg.KeyCode == System.Windows.Forms.Keys.Escape)
      {
        m_LineFeedback.Stop();
        m_LineFeedback = null;        
      }

      //enable snapping while the spacebar is depressed
      if (arg.KeyCode == System.Windows.Forms.Keys.Space)
      {
        m_SnappingEnvironment.Enabled = true;
      }

      // support changing the 
      if (arg.KeyCode == System.Windows.Forms.Keys.D)
      { 
        INumberDialog numDialog = new NumberDialogClass();
        bool isOK = numDialog.DoModal("Buffer Distance", 25, 3, ArcMap.Application.hWnd);
        if (isOK)
        {
          BufferDistance = numDialog.Value;
        }
      }

      base.OnKeyDown(arg);
    }
    protected override void OnKeyUp(KeyEventArgs arg)
    {
      //enable snapping while the spacebar is released
      if (arg.KeyCode == System.Windows.Forms.Keys.Space)
      {
        m_SnappingEnvironment.Enabled = false;
      }

      base.OnKeyDown(arg);
    }
    private ISnappingEnvironment GetSnappingEnvironment()
    {
      if (m_SnappingEnvironment == null)
      {
        IExtensionManager extensionManager = ArcMap.Application as IExtensionManager;

        if (extensionManager != null)
        {
          UID guid = new UIDClass();
          guid.Value = "{E07B4C52-C894-4558-B8D4-D4050018D1DA}";

          IExtension extension = extensionManager.FindExtension(guid);
          m_SnappingEnvironment = extension as ISnappingEnvironment;
        }
      }
      return m_SnappingEnvironment;
    }
    private IPoint QueryMapPoint(IScreenDisplay m_display, int X, int Y)
    {
      IDisplayTransformation dispTransformation = m_display.DisplayTransformation;

      IPoint point = dispTransformation.ToMapPoint(X, Y);

      return point;
    }
    private IGeometry Buffer(IGeometry inputGeom, double bufferDistance)
    {
      bufferConstruction = new BufferConstructionClass();
      IBufferConstructionProperties bufferProps = (IBufferConstructionProperties)bufferConstruction;
      bufferProps.EndOption = esriBufferConstructionEndEnum.esriBufferRound;
      bufferProps.GenerateCurves = true;
      
      return bufferConstruction.Buffer((IGeometry)inputGeom, dblBufferDistance);

      //IGeometryBag inputBuffer = new GeometryBagClass();
      //inputBuffer.SpatialReference = ArcMap.Editor.Map.SpatialReference;
      //Object missing = Type.Missing;
      //((IGeometryCollection)inputBuffer).AddGeometry((IGeometry)polyline, missing, missing);
      //IGeometryCollection outBuffer = new GeometryBagClass();
      //bufferConstruction.ConstructBuffers((IEnumGeometry)inputBuffer, 25, outBuffer);
      //return outBuffer.get_Geometry(0);
    }

    private double BufferDistance
    {
      get { return dblBufferDistance; }
      set { dblBufferDistance = value; }
    }

    private IBufferConstruction bufferConstruction;
    private double dblBufferDistance = 25;
    private IPoint m_CurrentMouseCoords;
    private ISnappingFeedback m_SnappingFeedback;
    private ISnappingEnvironment m_SnappingEnvironment;
    private IEditor3 m_editor;
    private INewLineFeedback m_LineFeedback = null;
  }

}
