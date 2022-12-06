# Turned Based Networking Demo
This repository is for demonstration purposes and is not suitable for production, nor is it guaranteed to run in Unity as-is.

## Overview
The premise of the game for this demo is a multiplayer turn-based match game where each player controls several characters they choose in the match lobby.  During the game, each character takes a turn sequentially, until they can no longer take more actions.  The demonstration shown here is using Mirror, an open source networking layer for Unity.  The implementation uses gated functions for client/server communication that will only compile and run in each respective environment.  The network design uses an authoritative server architecture for maintaining game-state and deciding the outcome of player decisions, which can involve randomized stat changes (eg. character damage, buffs, etc);

The Actions characters perform can be multi-part, with the server and each client sequentially running the Actions' phases until they are complete.  The Actions and event Signaling systems used in this demo are implemented elsewhere, but it does show the use of Unity Timelines to perform Action sequences.  This allows for layered cinematic actions to take place across the network.

## Technologies Used
* [Unity](https://unity.com/)
* [Mirror](https://mirror-networking.com/)
* [Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@1.8/manual/index.html)

## Concepts Demonstrated
* Authoritative Server architecture
* Game networking concepts
* Turn-based control-flow
