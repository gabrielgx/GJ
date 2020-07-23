using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Gaia
{
    
    /// <summary>
    /// Data structure to collect data about specific events that are called repeatedly while the stopwatch is running.
    /// </summary>
    [System.Serializable]
    public class GaiaStopWatchEvent
    {
        public string m_name;
        public string m_parent;
        public bool m_started;
        public int m_callCount;
        public long m_firstStartTimeStamp;
        public long m_lastStartTimeStamp;
        public long m_lastStopTimeStamp;
        public long m_durationPerCall;
        public long m_accumulatedTime;
    }


    //Simple static wrapper to access one Stopwatch instance across all Gaia
    //Can be used to measure time across multiple parts of the application
    //without having to deal with passing stopwatch instances around 
    public static class GaiaStopwatch
    {
        public static Stopwatch m_stopwatch = new Stopwatch();
        static long m_lastLogElapsed = 0;
        public static long m_accumulatedYieldTime = 0;
        static List<GaiaStopWatchEvent> m_events = new List<GaiaStopWatchEvent>();

        public static void Start()
        {
            m_events.Clear();
            m_stopwatch.Reset();
            m_stopwatch.Start();
        }

        public static void LogWithTime(string logText)
        {
            UnityEngine.Debug.Log(m_stopwatch.ElapsedMilliseconds.ToString() + " | Diff: " + (m_stopwatch.ElapsedMilliseconds - m_lastLogElapsed).ToString() + " | " + logText);
            m_lastLogElapsed = m_stopwatch.ElapsedMilliseconds;
        }

        public static void StartEvent(string name, string parent = "")
        {
            GaiaStopWatchEvent stopWatchEvent = m_events.Find(x => x.m_name == name);

            if (stopWatchEvent != null)
            {
                if (!stopWatchEvent.m_started)
                {
                    stopWatchEvent.m_lastStartTimeStamp = m_stopwatch.ElapsedMilliseconds;
                    stopWatchEvent.m_started = true;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Trying to start an event '" + name + "' with the Gaia Stopwatch that has already been started before!");
                }
            }
            else
            {
                //Event does not exist yet, let's create it
                stopWatchEvent = new GaiaStopWatchEvent()
                {
                    m_firstStartTimeStamp = m_stopwatch.ElapsedMilliseconds,
                    m_lastStartTimeStamp = m_stopwatch.ElapsedMilliseconds,
                    m_name = name,
                    m_started = true,
                };

                m_events.Add(stopWatchEvent);

            }

            //assign parent if one is given & no parent assigned yet
            if (string.IsNullOrEmpty(stopWatchEvent.m_parent) && !string.IsNullOrEmpty(parent))
            {
                if (m_events.Exists(x => x.m_name == parent))
                {
                    stopWatchEvent.m_parent = parent;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Trying to add an event '" + name + "' to the parent '" + parent + "' with the Gaia Stopwatch, but that parent event does not exist yet!");
                }
            }

        }

        public static void EndEvent(string name)
        {
            GaiaStopWatchEvent stopWatchEvent = m_events.Find(x => x.m_name == name);

            if (stopWatchEvent != null)
            {
                stopWatchEvent.m_lastStopTimeStamp = m_stopwatch.ElapsedMilliseconds;
                stopWatchEvent.m_started = false;
                stopWatchEvent.m_callCount++;
                stopWatchEvent.m_accumulatedTime += stopWatchEvent.m_lastStopTimeStamp - stopWatchEvent.m_lastStartTimeStamp;
                stopWatchEvent.m_durationPerCall = stopWatchEvent.m_accumulatedTime / stopWatchEvent.m_callCount;
            }
            else
            {
                UnityEngine.Debug.LogWarning("Trying to stop an event '" + name + "' with the Gaia Stopwatch, but that event does not exist yet!");
            }

        }

        public static void Stop(bool outputData = true)
        {
            //end any running events
            foreach (GaiaStopWatchEvent stopWatchEvent in m_events.FindAll(x=>x.m_started==true))
            {
                EndEvent(stopWatchEvent.m_name);
            }
            m_stopwatch.Stop();
            if (outputData)
            {
                GameObject parentGO = GaiaUtils.GetStopwatchDataObject();
                GameObject stopWatchDataObject = new GameObject(string.Format("Gaia Stopwatch Run {0:yyyy-MM-dd--HH-mm-ss}", DateTime.Now));
                stopWatchDataObject.transform.parent = parentGO.transform;
                GaiaStopwatchDataset newDataset = stopWatchDataObject.AddComponent<GaiaStopwatchDataset>();
                newDataset.m_events = m_events;
            }

        }
    }
}