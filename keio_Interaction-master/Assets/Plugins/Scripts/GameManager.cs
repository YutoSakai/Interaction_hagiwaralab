using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

public class GameManager : MonoBehaviour {

  [SerializeField] string FilePath;
  [SerializeField] string ClipPath;

  [SerializeField] Button Play;
  [SerializeField] Button SetChart;

  [SerializeField] GameObject Don;
  [SerializeField] GameObject Ka;

  [SerializeField] Transform SpawnPoint;
  [SerializeField] Transform BeatPoint;

  AudioSource Music;

  float PlayTime;
  float Distance;
  float During;
  bool isPlaying;
  int GoIndex;

  float CheckRange;
  float BeatRange;
  List<float> NoteTimings;

  string Title;
  int BPM;
  List<GameObject> Notes;

  Subject<string> SoundEffectSubject = new Subject<string>();

  public IObservable<string> OnSoundEffect {
     get { return SoundEffectSubject; }
  }

  // イベントを通知するサブジェクトを追加
  Subject<string> MessageEffectSubject = new Subject<string>();

  // イベントを検知するオブザーバーを追加
  public IObservable<string> OnMessageEffect {
     get { return MessageEffectSubject; }
  }

  void OnEnable() {
    Music = this.GetComponent<AudioSource>();

    Distance = Math.Abs(BeatPoint.position.x - SpawnPoint.position.x);
    During = 2 * 1000;
    isPlaying = false;
    GoIndex = 0;

    CheckRange = 120;
    BeatRange = 80;

    Play.onClick
      .AsObservable()
      .Subscribe(_ => play());

    SetChart.onClick
      .AsObservable()
      .Subscribe(_ => loadChart());

    this.UpdateAsObservable()
      .Where(_ => isPlaying)
      .Where(_ => Notes.Count > GoIndex)
      .Where(_ => Notes[GoIndex].GetComponent<NoteController>().getTiming() <= ((Time.time * 1000 - PlayTime) + During))
      .Subscribe(_ => {
        Notes[GoIndex].GetComponent<NoteController>().go(Distance, During);
        GoIndex++;
      });

    this.UpdateAsObservable()
      .Where(_ => isPlaying)
      .Where(_ => Input.GetKeyDown(KeyCode.D))
      .Subscribe(_ => {
        beat("don", Time.time * 1000 - PlayTime);
        SoundEffectSubject.OnNext("don");
      });

    this.UpdateAsObservable()
      .Where(_ => isPlaying)
      .Where(_ => Input.GetKeyDown(KeyCode.K))
      .Subscribe(_ => {
        beat("ka", Time.time * 1000 - PlayTime);
        SoundEffectSubject.OnNext("ka");
      });
  }

  void loadChart() {
    Notes = new List<GameObject>();
    NoteTimings = new List<float>();

    string jsonText = Resources.Load<TextAsset>(FilePath).ToString();
    Music.clip = (AudioClip)Resources.Load(ClipPath);

    JsonNode json = JsonNode.Parse(jsonText);
    Title = json["title"].Get<string>();
    BPM = int.Parse(json["bpm"].Get<string>());

    foreach(var note in json["notes"]) {
      string type = note["type"].Get<string>();
      float timing = float.Parse(note["timing"].Get<string>());

      GameObject Note;
      if (type == "don") {
        Note = Instantiate(Don, SpawnPoint.position, Quaternion.identity);
      } else if (type == "ka") {
        Note = Instantiate(Ka, SpawnPoint.position, Quaternion.identity);
      } else {
        Note = Instantiate(Don, SpawnPoint.position, Quaternion.identity); // default don
      }

      Note.GetComponent<NoteController>().setParameter(type, timing);

      Notes.Add(Note);
      NoteTimings.Add(timing);
    }
  }

  void play() {
    Music.Stop();
    Music.Play();
    PlayTime = Time.time * 1000;
    isPlaying = true;
    Debug.Log("Game Start!");
  }

  void beat(string type, float timing) {
    float minDiff = -1;
    int minDiffIndex = -1;

    for (int i = 0; i < Notes.Count; i++) {
      if(NoteTimings[i] > 0) {
        float diff = Math.Abs(NoteTimings[i] - timing);
        if(minDiff == -1 || minDiff > diff) {
          minDiff = diff;
          minDiffIndex = i;
        }
      }
    }

    if(minDiff != -1 & minDiff < CheckRange) {
      if(minDiff < BeatRange & Notes[minDiffIndex].GetComponent<NoteController>().getType() == type) {
        NoteTimings[minDiffIndex] = -1;
        Notes[minDiffIndex].SetActive(false);

        MessageEffectSubject.OnNext("good"); // イベントを通知
        Debug.Log("beat " + type + " success.");
      }
      else {
        NoteTimings[minDiffIndex] = -1;
        Notes[minDiffIndex].SetActive(false);

        MessageEffectSubject.OnNext("failure"); // イベントを通知
        Debug.Log("beat " + type + " failure.");
      }
    }
    else {
      Debug.Log("through");
    }
  }
}
