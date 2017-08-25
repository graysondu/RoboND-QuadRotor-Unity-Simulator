﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using SocketIO;
//using UnityStandardAssets.Vehicles.Car;
using System;
//using System.Security.AccessControl;
using UnityEngine.UI;
using System.Threading;

public class CommandServer : MonoBehaviour
{
	public QuadController quad;
	public Camera colorCam;
	public Camera depthCam;
	private SocketIOComponent _socket;

//	public RawImage inset3;

	Texture2D inset1Tex;
	Texture2D inset2Tex;
//	Texture2D inset3Tex;

	Thread broadcastThread;
	int broadcastFrequency;

	DateTime origin = new DateTime (1970, 1, 1, 0, 0, 0);

	void Start ()
	{
		_socket = GetComponent<SocketIOComponent> ();
		_socket.On ( "open", OnOpen );
		_socket.On ( "object_detected", OnObjectDetected );
		_socket.On ( "create_box_marker", OnCreateBoxMarker );
		_socket.On ( "delete_marker", OnDeleteMarker );
		inset1Tex = new Texture2D ( 1, 1 );
		inset2Tex = new Texture2D ( 1, 1 );
//		inset3Tex = new Texture2D ( 1, 1 );

		broadcastFrequency = 60;
		broadcastThread = new Thread ( ThreadFunc );
		broadcastThread.Start ();
		Debug.Log ( "starting" );
//		EmitTelemetry ();
	}

	void Update ()
	{
//		EmitTelemetry (); // sending in thread instead
	}

	void OnDestroy ()
	{
		broadcastThread.Abort ();
	}

	void ThreadFunc ()
	{
		int sleepTime = (int) ( 1000f / broadcastFrequency );

		while ( true )
		{
			EmitTelemetry ();
			Thread.Sleep ( sleepTime );
		}
	}

	void OnOpen(SocketIOEvent obj)
	{
		Debug.Log ( "Connection Open" );
		EmitTelemetry ();
	}

	void OnObjectDetected (SocketIOEvent obj)
	{
		Debug.Log ( "Object detected" );

		JSONObject json = obj.data;
		int[] camera_coords = {
			(int) json.GetField ( "x" ).n,
			(int) json.GetField ( "y" ).n
		};
		string name = json.GetField ( "object_name" ).str;
		double timestamp = json.GetField ( "timestamp" ).f;

		Dictionary<string, string> data = new Dictionary<string, string> ();
		data [ "action" ] = "object";
		data [ "name" ] = name;

		Ack ( new JSONObject ( data ) );
	}

	void OnCreateBoxMarker (SocketIOEvent obj)
	{
		Debug.Log ( "Create marker" );
		JSONObject json = obj.data;
		string id = json.GetField ( "id" ).str;
		string pose = json.GetField ( "pose" ).str;
//		float[] pose = {
//			json.GetField ( "x" ).f,
//			json.GetField ( "y" ).f,
//			json.GetField ( "z" ).f,
//			json.GetField ( "roll" ).f,
//			json.GetField ( "pitch" ).f,
//			json.GetField ( "yaw" ).f
//		};
		string color = json.GetField ( "color" ).str;
//		int[] color = {
//			json.GetField ( "r" ).n,
//			json.GetField ( "g" ).n,
//			json.GetField ( "b" ).n
//		};
		float alpha = json.GetField ( "alpha" ).f;
		float duration = json.GetField ( "seconds" ).f;

		Dictionary<string, string> data = new Dictionary<string, string> ();
		data [ "action" ] = "create";
		data [ "id" ] = id;

		Ack ( new JSONObject ( data ) );
	}

	void OnDeleteMarker (SocketIOEvent obj)
	{
		Debug.Log ( "Delete marker" );
		JSONObject json = obj.data;
		string id = json.GetField ( "id" ).str;

		Dictionary<string, string> data = new Dictionary<string, string> ();
		data [ "action" ] = "delete";
		data [ "id" ] = id;

		Ack ( new JSONObject ( data ) );
	}

	void Ack (JSONObject obj)
	{
		_socket.Emit ( "Ack", obj );
	}

	void EmitTelemetry ()
	{
//		Debug.Log ( "Emitting" );
		UnityMainThreadDispatcher.Instance ().Enqueue ( () =>
		{
//			print ( "Attempting to Send..." );

			// Collect Data from the Car
			Dictionary<string, string> data = new Dictionary<string, string> ();

			data [ "timestamp" ] = Timestamp ().ToString ();
			Vector3 v = quad.Position.ToRos ();
			Vector3 v2 = ( -quad.Rotation.eulerAngles ).ToRos ();
			data [ "pose" ] = v.x.ToString ( "N4" ) + "," + v.y.ToString ( "N4" ) + "," + v.z.ToString ( "N4" ) + "," + v2.x.ToString ( "N4" ) + "," + v2.y.ToString ( "N4" ) + "," + v2.z.ToString ( "N4" );
			data [ "rgb" ] = Convert.ToBase64String ( CameraHelper.CaptureFrame ( colorCam ) );
			data [ "depth" ] = Convert.ToBase64String ( CameraHelper.CaptureFrame ( depthCam ) );

//			Debug.Log ("sangle " + data["steering_angle"] + " vert " + data["vert_angle"] + " throt " + data["throttle"] + " speed " + data["speed"] + " image " + data["image"]);
			_socket.Emit ( "sensor_frame", new JSONObject ( data ) );
//			Debug.Log ("Emitted");
		} );
	}

	double Timestamp ()
	{
		return ( DateTime.UtcNow - origin ).TotalSeconds;
	}
}