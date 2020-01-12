using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class SoundEffectManager : MonoBehaviour {

  [SerializeField] GameManager GameManager;
  [SerializeField] AudioSource DonPlayer;
  [SerializeField] AudioSource KaPlayer;

  void OnEnable() {
    GameManager
      .OnSoundEffect
      .Where(type => type == "don")
      .Subscribe(type => donPlay());

    GameManager
      .OnSoundEffect
      .Where(type => type == "ka")
      .Subscribe(type => kaPlay());
  }

  void donPlay() {
    DonPlayer.Stop();
    DonPlayer.Play();
  }

  void kaPlay() {
    KaPlayer.Stop();
    KaPlayer.Play();
  }
}
