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
  [SerializeField] Text ScoreText;
  [SerializeField] Text ComboText;
  [SerializeField] Text TitleText;


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

  float ComboCount;
  float Score;
  float ScoreFirstTerm;
  float ScoreTorerance;
  float ScoreCeilingPoint;
  int CheckTimingIndex;

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

    ScoreCeilingPoint = 1050000;
    CheckTimingIndex = 0;

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
      .Where(_ => Notes.Count > CheckTimingIndex)
      .Where(_ => NoteTimings[CheckTimingIndex] == -1)
      .Subscribe(_ => CheckTimingIndex++);

    this.UpdateAsObservable()
      .Where(_ => isPlaying)
      .Where(_ => Notes.Count > CheckTimingIndex)
      .Where(_ => NoteTimings[CheckTimingIndex] != -1)
      .Where(_ => NoteTimings[CheckTimingIndex] < ((Time.time * 1000 - PlayTime) - CheckRange/2))
      .Subscribe(_ => {
        updateScore("failure");
        CheckTimingIndex++;
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

    TitleText.text = Title;

    if(Notes.Count < 10) {
      ScoreFirstTerm = (float)Math.Round(ScoreCeilingPoint/Notes.Count);
      ScoreTorerance = 0;
    } else if(10 <= Notes.Count && Notes.Count < 30) {
      ScoreFirstTerm = 300;
      ScoreTorerance = (float)Math.Floor((ScoreCeilingPoint - ScoreFirstTerm * Notes.Count)/(Notes.Count - 9));
    } else if(30 <= Notes.Count && Notes.Count < 50) {
      ScoreFirstTerm = 300;
      ScoreTorerance = (float)Math.Floor((ScoreCeilingPoint - ScoreFirstTerm * Notes.Count)/(2 * (Notes.Count - 19)));
    } else if(50 <= Notes.Count && Notes.Count < 100) {
      ScoreFirstTerm = 300;
      ScoreTorerance = (float)Math.Floor((ScoreCeilingPoint - ScoreFirstTerm * Notes.Count)/(4 * (Notes.Count - 39)));
    } else {
      ScoreFirstTerm = 300;
      ScoreTorerance = (float)Math.Floor((ScoreCeilingPoint - ScoreFirstTerm * Notes.Count)/(4 * (3 * Notes.Count - 232)));
    }
  }

  void play() {
    Music.Stop();
    Music.Play();
    PlayTime = Time.time * 1000 + 580; // ここで楽曲開始時間調整
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
        updateScore("good");
        // Debug.Log("beat " + type + " success.");
      }
      else {
        NoteTimings[minDiffIndex] = -1;
        Notes[minDiffIndex].SetActive(false);

        MessageEffectSubject.OnNext("failure"); // イベントを通知
        updateScore("false");
        // Debug.Log("beat " + type + " failure.");
      }
    }
    else {
      // Debug.Log("through");
    }
  }

  void updateScore(string result) {
    if(result == "good") {
      ComboCount++;

      float plusScore;
      if (ComboCount < 10) {
        plusScore = ScoreFirstTerm;
      }
      else if (10 <= ComboCount && ComboCount < 30) {
        plusScore = ScoreFirstTerm + ScoreTorerance;
      }
      else if (30 <= ComboCount && ComboCount < 50) {
        plusScore = ScoreFirstTerm + ScoreTorerance * 2;
      }
      else if (50 <= ComboCount && ComboCount < 100) {
        plusScore = ScoreFirstTerm + ScoreTorerance * 4;
      }
      else {
        plusScore = ScoreFirstTerm + ScoreTorerance * 8;
      }

      Score += plusScore;
    }
    else if (result == "failure") {
      ComboCount = 0;
    }
    else {
      ComboCount = 0; // default failure
    }

    ComboText.text = ComboCount.ToString();
    ScoreText.text = Score.ToString();
  }
}
