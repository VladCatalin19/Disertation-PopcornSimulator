# Disertation-PopcornSimulator

https://github.com/DavidArayan/ezy-slice


https://julien-tierny.github.io/stuff/papers/tierny_pg06.pdf

https://github.com/muckSponge/UnityAsync/

https://github.com/lumpn/unity-threading


Models:
- kernel : https://sketchfab.com/3d-models/corn-kernel-284db06b8d094a6382c62913083335d7
- kitchen : https://sketchfab.com/3d-models/kitchen-a7403be7a6cf4251b9b6fec4420e1b98
- pots : https://sketchfab.com/3d-models/pots-and-pans-kitchen-set-8fcb576caba0460b903a562e889a37b3

3d thinning algorithm

free form deformation


Sem 1:
- sa pun floricelele sa se expandeze intr-un castrol
- teste performanta
- studiu coliziuni unity

- de la sincron la asincron
- expansiune toate o dată sau unele random
- randomness la timpul de dat pe spate la partile pufoase
- asamblat scena


Sem 2:
- fine tuning
- mai multa configurare
- sa sara popcorn-ul
- texturare dinamica? interpolare in functie de animatie si pozitie



- Make popcorn jump.
? Fix lighting / normals for puffed surfaces.
+ Add new texture / submesh for puffed parts.
- Dynamic texture
? Fix bone weights.
- GPGPU collisions.
- Make mini stages more efficient.
- Better management for expanding popcorn.
- More configuration.


Optimizations:
 - see if collections can have preallocated sizes
 - use stackalloc for small arrays (maybe bones)



Mai multe varfuri pe schelet / mesh
Varfurile sa depinda de mai multe oase
Oasele in interiorul feliilor
Cum se fac coliziunile in Unity
