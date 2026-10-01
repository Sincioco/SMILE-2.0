# Neris orbital craft

These two original procedural models are specific to space travel: sealed cabins,
short thermal fins, ventral lift nozzles, rear ion drives, landing legs, teal heat
shielding and Neris gold compass crests. They are independent of the Horizon fleet
and the four alien visitors. The smaller shuttle is 30 metres long; the transport
is 44 metres long. Both keep four material parts and share models across instances.

`build_spacecraft.py` owns the Blender source, preview, native GLBs and checked
manifest. It uses the existing static exporter and requires no downloaded assets.
The nose is Blender -Y; native placement rotates the cooked model 180 degrees,
pointing away from the hangar. Landing feet have local minimum height zero.

NerisOrbitalFlight owns a staggered 160-second schedule: three shuttles start on
the aprons, reverse into the open hangars, emerge nose first, then take off and
land vertically. One larger transport visits the compass marking on the terminal
approach apron (native X -5500, Z -5640, at deck height), matching Sin's corrected
landing target. NerisOrbitalFleet owns two models and 16 objects;
the existing Spaceport preview owns their lifecycle. Blender exports show parked
instances at the same positions, including the occasional transport.

Traffic is visual ambient activity. It does not provide boarding, moving craft
collision, animated landing gear or hangar door simulation. The four established
alien-pad schedules and Horizon aircraft remain unchanged.

Validation: native session checks exercise parked/hangar/vertical-flight phases,
visibility intervals, the corrected apron anchor and height, and nose-out rendering.
Both GLBs meet the checked four-part budgets. Blender exports contain 122 source
objects and 16 landing feet at the correct decks. Sin confirmed the corrected
native transport landing on October 1 after the 20:48:48 Release build; three
parked shuttles were also inspected. Full-cycle visual observation was not performed.
