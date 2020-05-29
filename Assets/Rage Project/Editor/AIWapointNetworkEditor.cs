using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

[CustomEditor(typeof(AIWaypointNetwork))]
public class AIWapointNetworkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AIWaypointNetwork network = (AIWaypointNetwork)target;
        network.DisplayMode = (PathDisplayMode)EditorGUILayout.EnumPopup("Display Mode", network.DisplayMode);
        if(network.DisplayMode == PathDisplayMode.Paths)
        {
            network.UIStart = EditorGUILayout.IntSlider("Start Waypoint", network.UIStart, 0, network.Waypoints.Count - 1);
            network.UIEnd = EditorGUILayout.IntSlider("End Waypoint", network.UIEnd, 0, network.Waypoints.Count - 1);
        }
        DrawDefaultInspector();
    }
    private void OnSceneGUI()
    {
        AIWaypointNetwork network = (AIWaypointNetwork)target;
        for (int i = 0; i<network.Waypoints.Count; i++)
        {
            if(network.Waypoints[i] != null)
            {
                Handles.color = Color.red;
                Handles.Label(network.Waypoints[i].position, "Waypoint " + i.ToString());
            }
        }
        if(network.DisplayMode == PathDisplayMode.Connections)
        {
            Vector3[] linPoints = new Vector3[network.Waypoints.Count+1];

            for (int i = 0; i <= network.Waypoints.Count; i++)
            {
                int index = i != network.Waypoints.Count ? i : 0;
                if (network.Waypoints[index] != null)
                    linPoints[i] = network.Waypoints[index].position;
                else
                    linPoints[i] = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            }
            Handles.color = Color.cyan;
            Handles.DrawPolyLine(linPoints);
        }
        else if (network.DisplayMode == PathDisplayMode.Paths)
        {
            NavMeshPath path = new NavMeshPath();
            if(network.Waypoints[network.UIStart] != null && network.Waypoints[network.UIEnd] != null)
            {
                Vector3 from = network.Waypoints[network.UIStart].position;
                Vector3 to = network.Waypoints[network.UIEnd].position;

                NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
                Handles.color = Color.red;
                Handles.DrawPolyLine(path.corners);
            }

        }
    }
}
