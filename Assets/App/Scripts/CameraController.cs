﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NRKernal
{
    public class CameraController : MonoBehaviour
    {

        public static CameraController Instance;

        //WebCamTexture webcamTexture;
        //Renderer renderer;

        [SerializeField] Picture picturePrefab;
        [SerializeField] Recording recordingPrefab;

        [SerializeField] GameObject anchor;
        [SerializeField] GameObject lookAt;

        private GameObject currentSnapshot;

        private bool isRecording;

        AudioClip tempRecording;
        AudioSource tempAudioSource;
        private float startRecordingTime;

        private Recording recordingActive;


        //[SerializeField] Renderer screenshotRenderer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            //renderer = GetComponent<Renderer>();
            tempAudioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            //OpenCamera();
            isRecording = false;
        }

        private void OnEnable()
        {
            TriggerButton.PressedDownCallback += TakePicture;
            //PlaceSnapshotButton.PressedDownCallback += PlaceSnapshot;
            
            PlaceSnapshotButton.PressedDownCallback += BeginPlace;
            PlaceSnapshotButton.PressedUpCallback += EndPlace;
            
            RecordingButton.PressedDownCallback += StartRecording;
            RecordingButton.PressedUpCallback += EndRecording;
            DeleteButton.PressedDownCallback += DeleteSnapshot;
        }

        private void OnDisable()
        {
            TriggerButton.PressedDownCallback -= TakePicture;
            //PlaceSnapshotButton.PressedDownCallback -= PlaceSnapshot;

            PlaceSnapshotButton.PressedDownCallback -= BeginPlace;
            PlaceSnapshotButton.PressedUpCallback -= EndPlace;

            RecordingButton.PressedDownCallback -= StartRecording;
            RecordingButton.PressedUpCallback -= EndRecording;
            DeleteButton.PressedDownCallback -= DeleteSnapshot;

        }

        private void Update()
        {
            
            if (Physics.Raycast(lookAt.transform.position, -lookAt.transform.up, out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.collider.GetComponent<Recording>() && recordingActive == null)
                {

                    

                    recordingActive = hit.collider.GetComponent<Recording>();

                    recordingActive.StartPlayback();
                }
            }
            else
            {
                if (recordingActive != null)
                {
                    recordingActive.StopPlayback();
                    recordingActive = null;
                }
            }
        }

        // Open the camera
        public void OpenCamera()
        {
            //webcamTexture = new WebCamTexture();
            //renderer.material.mainTexture = webcamTexture;
            //webcamTexture.Play();
        }

        private void TakePicture()
        {
            StartCoroutine(TakePhotoRoutine());
        }

        private IEnumerator TakePhotoRoutine()  // Start this Coroutine on some button click
        {
            if (currentSnapshot == null)
            {
                // NOTE - you almost certainly have to do this here:

                yield return new WaitForEndOfFrame();

                // it's a rare case where the Unity doco is pretty clear,
                // http://docs.unity3d.com/ScriptReference/WaitForEndOfFrame.html
                // be sure to scroll down to the SECOND long example on that doco page 

                WebCamTexture webcamTexture = new WebCamTexture();
                webcamTexture = CameraTexture.Instance.webcamTexture;


                Texture2D photo = new Texture2D(webcamTexture.width, webcamTexture.height);
                photo.SetPixels(webcamTexture.GetPixels());
                photo.Apply();

                currentSnapshot = Instantiate(picturePrefab.gameObject) as GameObject;

                currentSnapshot.GetComponent<Snapshot>().SetFollowTransform(anchor.transform);
                currentSnapshot.GetComponent<Snapshot>().SetLookAtTransform(lookAt.transform);



                currentSnapshot.GetComponent<Picture>().SetQuadTexture(photo);

                //currentSnapshot.GetComponent<Renderer>().material.mainTexture = photo;

                //screenshotRenderer.material.mainTexture = photo;

                //Encode to a PNG
                //byte[] bytes = photo.EncodeToPNG();
                //Write out the PNG. Of course you have to substitute your_path for something sensible
                //File.WriteAllBytes(your_path + "photo.png", bytes);
            }
        }

        private void PlaceSnapshot()
        {
            if (currentSnapshot != null)
            {
                currentSnapshot.GetComponent<Snapshot>().SetFollowTransform(null);
                currentSnapshot.GetComponent<Snapshot>().SetLookAtTransform(null);

                currentSnapshot = null;
            }
            else
            {
                if (Physics.Raycast(lookAt.transform.position, -lookAt.transform.up, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.collider.CompareTag("Screenshot"))
                    {
                        currentSnapshot = hit.collider.gameObject;

                        currentSnapshot.GetComponent<Snapshot>().SetFollowTransform(anchor.transform);
                        currentSnapshot.GetComponent<Snapshot>().SetLookAtTransform(lookAt.transform);
                    }
                }
            }
        }

        private void BeginPlace()
        {
            if (currentSnapshot == null)
            {
                if (Physics.Raycast(lookAt.transform.position, -lookAt.transform.up, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.collider.CompareTag("Screenshot"))
                    {
                        currentSnapshot = hit.collider.gameObject;

                        currentSnapshot.GetComponent<Snapshot>().SetFollowTransform(anchor.transform);
                        currentSnapshot.GetComponent<Snapshot>().SetLookAtTransform(lookAt.transform);
                    }
                }
            }

            
        }

        private void EndPlace()
        {
            if (currentSnapshot != null)
            {
                currentSnapshot.GetComponent<Snapshot>().SetFollowTransform(null);
                currentSnapshot.GetComponent<Snapshot>().SetLookAtTransform(null);

                currentSnapshot = null;
            }
        }

        private void DeleteSnapshot()
        {
            if (currentSnapshot == null)
            {
                if (Physics.Raycast(lookAt.transform.position, -lookAt.transform.up, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.collider.CompareTag("Screenshot"))
                    {
                        Destroy(hit.collider.gameObject);
                    }
                }
            }
        }

        private GameObject CreateScreenshotPrefab()
        {
            var gO = Instantiate(picturePrefab.gameObject) as GameObject;

            gO.GetComponent<Snapshot>().SetFollowTransform(anchor.transform);

            return gO;
        }

        private void Record()
        {
            //if (currentSnapshot == null)
            {
                //currentSnapshot = new GameObject();

                if (!isRecording)
                {
                    StartRecording();
                }
                else
                {
                    EndRecording();
                }
            }
        }

        private void StartRecording()
        {
            if (!isRecording)
            {
                //Get the max frequency of a microphone, if it's less than 44100 record at the max frequency, else record at 44100
                int minFreq;
                int maxFreq;
                int freq = 44100;
                Microphone.GetDeviceCaps("", out minFreq, out maxFreq);
                if (maxFreq < 44100)
                    freq = maxFreq;

                //Start the recording, the length of 300 gives it a cap of 5 minutes
                tempRecording = Microphone.Start("", false, 300, 44100);
                startRecordingTime = Time.time;

                //tempAudioSource.clip = Microphone.Start(null, false, 30, 44100);
                isRecording = true;
            }
            
        }

        private void EndRecording()
        {
            if (isRecording)
            {
                //Microphone.End(Microphone.devices[0])/
                //End the recording when the mouse comes back up, then play it
                Microphone.End("");

                //Trim the audioclip by the length of the recording
                AudioClip recordingNew = AudioClip.Create(tempRecording.name, (int)((Time.time - startRecordingTime) * tempRecording.frequency), tempRecording.channels, tempRecording.frequency, false);
                float[] data = new float[(int)((Time.time - startRecordingTime) * tempRecording.frequency)];
                tempRecording.GetData(data, 0);
                recordingNew.SetData(data, 0);
                this.tempRecording = recordingNew;

                currentSnapshot = Instantiate(recordingPrefab.gameObject) as GameObject;

                currentSnapshot.GetComponent<Snapshot>().SetFollowTransform(anchor.transform);
                currentSnapshot.GetComponent<Snapshot>().SetLookAtTransform(lookAt.transform);

                currentSnapshot.GetComponent<Recording>().SetAudioClip(tempRecording);

                isRecording = false;
            }

        }
    }
}

