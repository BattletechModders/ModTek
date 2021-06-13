# Custom Sounds

since 0.7.6.7 ModTek supports loading Wwise sound banks definitions
  example:
```
{
  "name": "Screams",
  "filename": "Screams.bnk",
  "type": "Combat",
  "volumeRTPCIds":[2081458675],
  "volumeShift": 0,
  "events":{
    "scream01":147496415
  }
}
```
  **name** - is unique name of sound bank<br/>
  **filename** - is name of file containing real audio content. Battletech is using Wwise 2016.2<br/>
  **type** - type of sound bank. <br/>
    Available types:<br/>
* **Combat** - banks of this type always loading at combat start and unloading at combat end. Should be used for sounds played on battlefield.<br/>
* **Default** - banks of this type loading at game start. Moistly used for UI <br/>
* **Voice** - banks of this type contains pilot's voices<br/>

**events** - map of events exported in bank. Needed for events can be referenced from code via WwiseManager.PostEvent which takes this name as parameter<br/>
**volumeRTPCIds** - list of RTPC ids controlling loudness of samples. Combat sound banks controlled by effect settings volume slider, Voice by voice<br/>
