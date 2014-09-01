using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;

namespace MapSARConfig
{
  /// <summary>
  /// ConfigureSARExt class implementing custom ESRI Editor Extension functionalities.
  /// </summary>
  public class ConfigureSARExt : ESRI.ArcGIS.Desktop.AddIns.Extension
  {
    public ConfigureSARExt()
    {
    }

    protected override void OnStartup()
    {
      m_editor = ArcMap.Editor as IEditor3;
      Events.OnStartEditing += new IEditEvents_OnStartEditingEventHandler(Events_OnStartEditing);
      Events.OnCreateFeature += new IEditEvents_OnCreateFeatureEventHandler(Events_OnCreateFeature);
    }
    
    protected override void OnShutdown()
    {
      Events.OnStartEditing -= new IEditEvents_OnStartEditingEventHandler(Events_OnStartEditing);
      Events.OnCreateFeature -= new IEditEvents_OnCreateFeatureEventHandler(Events_OnCreateFeature);
    }

    #region Editor Events

    #region Shortcut properties to the various editor event interfaces
    private IEditEvents_Event Events
    {
      get { return ArcMap.Editor as IEditEvents_Event; }
    }
    private IEditEvents2_Event Events2
    {
      get { return ArcMap.Editor as IEditEvents2_Event; }
    }
    private IEditEvents3_Event Events3
    {
      get { return ArcMap.Editor as IEditEvents3_Event; }
    }
    private IEditEvents4_Event Events4
    {
      get { return ArcMap.Editor as IEditEvents4_Event; }
    }
    #endregion

    void Events_OnStartEditing()
    {
      try 
    	{
        ConfigureClassesForAttribution();

	    }
	    catch (Exception)
	    {		
		    throw;
	    }
      
    }     
    void Events_OnCreateFeature(ESRI.ArcGIS.Geodatabase.IObject obj)
    {

      //Populate the new search assignment feature with the appropriate search segment name
      if (m_editor.CurrentTemplate.Layer.Name == "Team Assignments")
      {
        IFeature NewSearchAssignmentFeature = obj as IFeature;
        NewSearchAssignmentFeature.set_Value( 
          NewSearchAssignmentFeature.Fields.FindFieldByAliasName(""), QuerySearchSegments());
        QuerySearchSegments();
      }      

    }


    private void ConfigureClassesForAttribution()
    {
      UID featurelayerUID = new UIDClass();
      featurelayerUID.Value = "{E156D7E5-22AF-11D3-9F99-00C04F6BC78E}";
      IEnumLayer layers = m_editor.Map.get_Layers(featurelayerUID, true);

      IGeoFeatureLayer geoFeatureLayer = layers.Next() as IGeoFeatureLayer;

      //turn on nonversioned editing so the dialog will appear at create time
      IEditAttributeProperties editProps3 = (IEditAttributeProperties)m_editor;
      editProps3.NonversionedAttributionEnabled = true;
      editProps3.AttributeEditAll = false;

      ISet sarSet = new ESRI.ArcGIS.esriSystem.SetClass();
      
      //find assets, clues_point, found, resource status, PLS
      while (geoFeatureLayer != null)
      {
        if (geoFeatureLayer.Name == "Clues_Point" || geoFeatureLayer.Name == "Found" || geoFeatureLayer.Name == "Assets" ||
          geoFeatureLayer.Name == "PLS" || geoFeatureLayer.Name == "Resource Status")
        {
          sarSet.Add(geoFeatureLayer.FeatureClass);
        }

        editProps3.AttributeEditClasses = sarSet;

        geoFeatureLayer = layers.Next() as IGeoFeatureLayer;
      }
      
    }

    private string QuerySearchSegments()
    {
      if (m_SearchSegmentLayer == null)
      {
        m_SearchSegmentLayer = ReturnFeatureLayerInMap("Search_Segments");
      }

      //set up query against new search assignment
      ISpatialFilter SpatialFilter = new SpatialFilterClass();
      SpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
      SpatialFilter.GeometryField = m_SearchSegmentLayer.FeatureClass.ShapeFieldName;

      //execute spatial search based on new geometry
      IFeatureCursor SearchSegmentsFeatureCursor = m_SearchSegmentLayer.Search(SpatialFilter, false);

      IFeature SearchSegmentFeature = SearchSegmentsFeatureCursor.NextFeature();

      short SearchSegmentCount = 0;
      string SearchSegmentDesignation = String.Empty;

      while (SearchSegmentFeature != null)
      {
        SearchSegmentCount++;
        SearchSegmentDesignation = (string)SearchSegmentFeature.get_Value(
          SearchSegmentFeature.Fields.FindFieldByAliasName("Segment Name"));
        SearchSegmentFeature = SearchSegmentsFeatureCursor.NextFeature();
      }

      //check to see if we have *one* search segment underneath
      if (SearchSegmentCount == 1)
      {
        return SearchSegmentDesignation;     
      }

      return string.Empty;
    }

    private IGeoFeatureLayer ReturnFeatureLayerInMap(string LayerName)
    {
      UID featurelayerUID = new UIDClass();
      featurelayerUID.Value = "{E156D7E5-22AF-11D3-9F99-00C04F6BC78E}";
      IEnumLayer layers = m_editor.Map.get_Layers(featurelayerUID, true);

      IGeoFeatureLayer geoFeatureLayer = layers.Next() as IGeoFeatureLayer;

      while (geoFeatureLayer != null)
      {
        if (geoFeatureLayer.Name == LayerName)
        {
          return geoFeatureLayer;
        }

        geoFeatureLayer = layers.Next() as IGeoFeatureLayer;
      }

      return null;
    }

    #endregion

    private IFeatureLayer m_SearchAssignmentLayer;
    private IFeatureLayer m_SearchSegmentLayer;
    private IEditor3 m_editor;
  }

}
