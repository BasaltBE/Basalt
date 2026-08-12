#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum LevelSoundEvent {
    item_use_on = 0,
    hit = 1,
    step = 2,
    step_baby = 221,
    fly = 3,
    jump = 4,
    jump_prevent = 287,
    Break = 5,
    place = 6,
    heavy_step = 7,
    gallop = 8,
    fall = 9,
    hurt = 17,
    hurt_baby = 219,
    hurt_in_water = 18,
    death = 14,
    death_baby = 220,
    death_in_water = 15,
    death_to_zombie = 16,
    ambient = 10,
    ambient_baby = 11,
    ambient_in_water = 12,
    ambient_in_air = 492,
    ambient_tame = 242,
    ambient_pollinate = 288,
    breathe = 13,
    mad = 19,
    boost = 20,
    bow = 21,
    squish_big = 22,
    squish_small = 23,
    fall_big = 24,
    fall_small = 25,
    splash = 26,
    fizz = 27,
    flap = 28,
    swim = 29,
    drink = 30,
    drink_honey = 294,
    drink_milk = 432,
    eat = 31,
    takeoff = 32,
    shake = 33,
    plop = 34,
    land = 35,
    saddle = 36,
    armor = 37,
    mob_armor_stand_place = 38,
    add_chest = 39,
    Throw = 40,
    attack = 41,
    attack_nodamage = 42,
    attack_strong = 43,
    warn = 44,
    shear = 45,
    milk = 46,
    thunder = 47,
    explode = 48,
    fire = 49,
    ignite = 50,
    fuse = 51,
    stare = 52,
    spawn = 53,
    born = 223,
    shoot = 54,
    break_block = 55,
    launch = 56,
    blast = 57,
    large_blast = 58,
    twinkle = 59,
    remedy = 60,
    unfect = 61,
    convert_to_drowned = 211,
    levelup = 62,
    bow_hit = 63,
    bullet_hit = 64,
    extinguish_fire = 65,
    item_fizz = 66,
    chest_open = 67,
    chest_closed = 68,
    shulkerbox_open = 69,
    shulkerbox_closed = 70,
    enderchest_open = 71,
    enderchest_closed = 72,
    power_on = 73,
    power_off = 74,
    attach = 75,
    detach = 76,
    deny = 77,
    tripod = 78,
    pop = 79,
    drop_slot = 80,
    note = 81,
    thorns = 82,
    piston_in = 83,
    piston_out = 84,
    portal = 85,
    water = 86,
    lava_pop = 87,
    lava = 88,
    beacon_activate = 229,
    beacon_ambient = 230,
    beacon_deactivate = 231,
    beacon_power = 232,
    conduit_activate = 233,
    conduit_ambient = 234,
    conduit_attack = 235,
    conduit_deactivate = 236,
    conduit_short = 237,
    bubble_pop = 216,
    bubble_up = 214,
    bubble_upinside = 217,
    bubble_down = 215,
    bubble_downinside = 218,
    burp = 89,
    bucket_fill_water = 90,
    bucket_empty_water = 92,
    bucket_fill_lava = 91,
    bucket_empty_lava = 93,
    bucket_fill_fish = 212,
    bucket_empty_fish = 213,
    armor_equip_chain = 94,
    armor_equip_diamond = 95,
    armor_equip_elytra = 100,
    armor_equip_generic = 96,
    armor_equip_gold = 97,
    armor_equip_iron = 98,
    armor_equip_leather = 99,
    armor_equip_netherite = 317,
    record_13 = 101,
    record_cat = 102,
    record_blocks = 103,
    record_chirp = 104,
    record_creator = 527,
    record_creator_music_box = 528,
    record_far = 105,
    record_mall = 106,
    record_mellohi = 107,
    record_stal = 108,
    record_strad = 109,
    record_ward = 110,
    record_11 = 111,
    record_wait = 112,
    record_null = 113,
    record_pigstep = 314,
    record_precipice = 529,
    record_relic = 469,
    record_otherside = 371,
    record_5 = 439,
    record_tears = 555,
    record_lava_chicken = 562,
    flop = 114,
    elderguardian_curse = 115,
    teleport = 118,
    shulker_open = 119,
    shulker_close = 120,
    mob_warning = 116,
    mob_warning_baby = 117,
    haggle = 121,
    haggle_yes = 122,
    haggle_no = 123,
    haggle_idle = 124,
    disappeared = 430,
    reappeared = 431,
    chorusgrow = 125,
    chorusdeath = 126,
    glass = 127,
    potion_brewed = 128,
    cast_spell = 129,
    prepare_attack = 130,
    prepare_summon = 131,
    prepare_wololo = 132,
    fang = 133,
    charge = 134,
    camera_take_picture = 135,
    leashknot_break = 137,
    leashknot_place = 136,
    growl = 138,
    whine = 139,
    pant = 140,
    purr = 141,
    purreow = 142,
    death_min_volume = 143,
    death_mid_volume = 144,
    imitate_blaze = 145,
    imitate_cave_spider = 146,
    imitate_creeper = 147,
    imitate_elder_guardian = 148,
    imitate_ender_dragon = 149,
    imitate_enderman = 150,
    imitate_endermite = 151,
    imitate_evocation_illager = 152,
    imitate_ghast = 153,
    imitate_husk = 154,
    imitate_magma_cube = 156,
    imitate_polar_bear = 157,
    imitate_shulker = 158,
    imitate_silverfish = 159,
    imitate_skeleton = 160,
    imitate_slime = 161,
    imitate_spider = 162,
    imitate_stray = 163,
    imitate_vex = 164,
    imitate_vindication_illager = 165,
    imitate_witch = 166,
    imitate_wither = 167,
    imitate_wither_skeleton = 168,
    imitate_wolf = 169,
    imitate_zombie = 170,
    imitate_zombie_pigman = 171,
    imitate_zombie_villager = 172,
    block_end_portal_frame_fill = 173,
    block_end_portal_spawn = 174,
    random_anvil_use = 175,
    bottle_dragonbreath = 176,
    balloonpop = 190,
    sparkler_active = 210,
    item_trident_hit = 178,
    item_trident_hit_ground = 185,
    item_trident_return = 179,
    item_trident_riptide_1 = 180,
    item_trident_riptide_2 = 181,
    item_trident_riptide_3 = 182,
    item_trident_throw = 183,
    item_trident_thunder = 184,
    block_fletching_table_use = 187,
    elemconstruct_open = 188,
    icebomb_hit = 189,
    lt_reaction_icebomb = 191,
    lt_reaction_bleach = 192,
    lt_reaction_epaste = 193,
    lt_reaction_epaste2 = 194,
    lt_reaction_fertilizer = 199,
    lt_reaction_fireball = 200,
    lt_reaction_mgsalt = 201,
    lt_reaction_miscfire = 202,
    lt_reaction_fire = 203,
    lt_reaction_miscexplosion = 204,
    lt_reaction_miscmystical = 205,
    lt_reaction_miscmystical2 = 206,
    lt_reaction_product = 207,
    sparkler_use = 208,
    glowstick_use = 209,
    block_turtle_egg_break = 224,
    block_turtle_egg_crack = 225,
    block_turtle_egg_hatch = 226,
    block_turtle_egg_attack = 228,
    block_sniffer_egg_crack = 466,
    block_sniffer_egg_hatch = 467,
    block_frog_spawn_hatch = 433,
    block_frog_spawn_break = 435,
    swoop = 238,
    presneeze = 240,
    sneeze = 241,
    scared = 243,
    ambient_aggressive = 252,
    ambient_worried = 253,
    cant_breed = 254,
    block_scaffolding_climb = 244,
    block_bamboo_sapling_place = 239,
    crossbow_loading_start = 245,
    crossbow_loading_middle = 246,
    crossbow_loading_end = 247,
    crossbow_shoot = 248,
    crossbow_quick_charge_start = 249,
    crossbow_quick_charge_middle = 250,
    crossbow_quick_charge_end = 251,
    item_shield_block = 255,
    portal_travel = 177,
    item_book_put = 256,
    block_grindstone_use = 257,
    block_bell_hit = 258,
    block_campfire_crackle = 259,
    block_sweet_berry_bush_hurt = 262,
    block_sweet_berry_bush_pick = 263,
    block_stonecutter_use = 265,
    block_cartography_table_use = 264,
    block_composter_empty = 266,
    block_composter_fill = 267,
    block_composter_fill_success = 268,
    block_composter_ready = 269,
    roar = 260,
    stun = 261,
    block_barrel_open = 270,
    block_barrel_close = 271,
    raid_horn = 272,
    ui_stonecutter_take_result = 276,
    ui_cartography_table_take_result = 275,
    ui_loom_take_result = 277,
    block_smoker_smoke = 278,
    block_blastfurnace_fire_crackle = 279,
    block_smithing_table_use = 280,
    block_loom_use = 273,
    ambient_in_raid = 274,
    screech = 281,
    sleep = 282,
    block_furnace_lit = 283,
    convert_mooshroom = 284,
    milk_suspiciously = 285,
    celebrate = 286,
    block_beehive_enter = 290,
    block_beehive_exit = 291,
    block_beehive_shear = 293,
    block_beehive_work = 292,
    block_beehive_drip = 289,
    ambient_cave = 295,
    angry = 302,
    retreat = 296,
    converted_to_zombified = 297,
    step_lava = 299,
    tempt = 300,
    panic = 301,
    admire = 298,
    particle_soul_escape_quiet = 312,
    particle_soul_escape_loud = 313,
    respawn_anchor_charge = 308,
    respawn_anchor_deplete = 309,
    respawn_anchor_set_spawn = 310,
    respawn_anchor_ambient = 311,
    ambient_crimson_forest_mood = 307,
    ambient_warped_forest_mood = 303,
    ambient_soulsand_valley_mood = 304,
    ambient_nether_wastes_mood = 305,
    ambient_crimson_forest_additions = 327,
    ambient_warped_forest_additions = 323,
    ambient_soulsand_valley_additions = 324,
    ambient_nether_wastes_additions = 325,
    ambient_basalt_deltas_additions = 326,
    ambient_crimson_forest_loop = 322,
    ambient_warped_forest_loop = 318,
    ambient_soulsand_valley_loop = 319,
    ambient_nether_wastes_loop = 320,
    ambient_basalt_deltas_loop = 321,
    lodestone_compass_link_compass_to_lodestone = 315,
    ambient_basalt_deltas_mood = 306,
    power_on_sculk_sensor = 328,
    power_off_sculk_sensor = 329,
    smithing_table_use = 316,
    Default = 186,
    lay_egg = 227,
    lay_spawn = 434,
    bucket_fill_powder_snow = 330,
    bucket_empty_powder_snow = 331,
    cauldron_drip_water_pointed_dripstone = 332,
    cauldron_drip_lava_pointed_dripstone = 333,
    tilt_down_big_dripleaf = 337,
    tilt_up_big_dripleaf = 338,
    drip_water_pointed_dripstone = 334,
    pick_berries_cave_vines = 336,
    drip_lava_pointed_dripstone = 335,
    copper_wax_on = 339,
    copper_wax_off = 340,
    scrape = 341,
    item_spyglass_use = 345,
    item_spyglass_stop_using = 346,
    chime_amethyst_block = 347,
    mob_player_hurt_drown = 342,
    mob_player_hurt_on_fire = 343,
    mob_player_hurt_freeze = 344,
    ambient_screamer = 348,
    hurt_screamer = 349,
    death_screamer = 350,
    milk_screamer = 351,
    jump_to_block = 352,
    pre_ram = 353,
    pre_ram_screamer = 354,
    ram_impact = 355,
    ram_impact_screamer = 356,
    squid_ink_squirt = 357,
    glow_squid_ink_squirt = 358,
    convert_to_stray = 359,
    cake_add_candle = 360,
    extinguish_candle = 361,
    ambient_candle = 362,
    block_click = 363,
    block_click_fail = 364,
    block_sculk_catalyst_bloom = 365,
    block_sculk_shrieker_shriek = 366,
    nearby_close = 367,
    nearby_closer = 368,
    nearby_closest = 369,
    agitated = 370,
    listening = 375,
    heartbeat = 376,
    tongue = 372,
    item_given = 428,
    item_taken = 429,
    item_thrown = 438,
    irongolem_crack = 373,
    irongolem_repair = 374,
    horn_break = 377,
    horn_call0 = 383,
    horn_call1 = 384,
    horn_call2 = 385,
    horn_call3 = 386,
    horn_call4 = 387,
    horn_call5 = 388,
    horn_call6 = 389,
    horn_call7 = 390,
    imitate_warden = 426,
    listening_angry = 427,
    sonic_boom = 436,
    sonic_charge = 437,
    convert_to_frog = 440,
    block_sculk_spread = 379,
    charge_sculk = 380,
    block_sculk_sensor_place = 381,
    block_sculk_shrieker_place = 382,
    block_enchanting_table_use = 442,
    bundle_drop_contents = 445,
    bundle_insert = 446,
    bundle_insert_fail = 533,
    bundle_remove_one = 447,
    step_sand = 443,
    dash_ready = 444,
    pressure_plate_click_off = 448,
    pressure_plate_click_on = 449,
    button_click_off = 450,
    button_click_on = 451,
    door_open = 452,
    door_close = 453,
    trapdoor_open = 454,
    trapdoor_close = 455,
    fence_gate_open = 456,
    fence_gate_close = 457,
    insert = 458,
    pickup = 459,
    insert_enchanted = 460,
    pickup_enchanted = 461,
    shatter_pot = 464,
    break_pot = 465,
    brush = 462,
    brush_completed = 463,
    block_sign_waxed_interact_fail = 468,
    note_bass = 470,
    pumpkin_carve = 471,
    mob_husk_convert_to_zombie = 472,
    mob_pig_death = 473,
    mob_hoglin_converted_to_zombified = 474,
    ambient_underwater_enter = 475,
    ambient_underwater_exit = 476,
    bottle_fill = 477,
    bottle_empty = 478,
    block_decorated_pot_insert = 481,
    block_decorated_pot_insert_fail = 482,
    crafter_craft = 479,
    crafter_fail = 480,
    crafter_disable_slot = 483,
    block_copper_bulb_turn_on = 490,
    block_copper_bulb_turn_off = 491,
    breeze_wind_charge_burst = 493,
    imitate_breeze = 494,
    trial_spawner_open_shutter = 484,
    trial_spawner_detect_player = 486,
    trial_spawner_close_shutter = 488,
    trial_spawner_spawn_mob = 487,
    trial_spawner_eject_item = 485,
    trial_spawner_ambient = 489,
    mob_armadillo_brush = 495,
    mob_armadillo_scute_drop = 496,
    armor_equip_wolf = 497,
    armor_unequip_wolf = 498,
    reflect = 499,
    vault_open_shutter = 500,
    vault_close_shutter = 501,
    vault_eject_item = 502,
    vault_insert_item = 503,
    vault_insert_item_fail = 504,
    vault_ambient = 505,
    vault_activate = 506,
    vault_deactivate = 507,
    hurt_reduced = 508,
    wind_charge_burst = 509,
    armor_break_wolf = 512,
    armor_crack_wolf = 511,
    armor_repair_wolf = 513,
    mace_smash_air = 514,
    mace_smash_ground = 515,
    mace_heavy_smash_ground = 520,
    trial_spawner_charge_activate = 516,
    trial_spawner_ambient_ominous = 517,
    apply_effect_bad_omen = 523,
    apply_effect_raid_omen = 524,
    apply_effect_trial_omen = 525,
    ominous_item_spawner_spawn_item = 518,
    ominous_bottle_end_use = 519,
    ominous_item_spawner_spawn_item_begin = 521,
    ominous_item_spawner_about_to_spawn_item = 526,
    imitate_bogged = 510,
    vault_reject_rewarded_player = 530,
    imitate_drowned = 531,
    sponge_absorb = 534,
    imitate_creaking = 532,
    block_creaking_heart_trail = 536,
    creaking_heart_spawn = 537,
    activate = 538,
    deactivate = 539,
    freeze = 540,
    unfreeze = 541,
    open = 542,
    open_long = 543,
    close = 544,
    close_long = 545,
    imitate_phantom = 546,
    imitate_zoglin = 547,
    imitate_guardian = 548,
    imitate_ravager = 549,
    imitate_pillager = 550,
    place_in_water = 551,
    state_change = 552,
    imitate_happy_ghast = 553,
    armor_unequip_generic = 554,
    ambient_weather_the_end_light_flash = 556,
    lead_leash = 557,
    lead_unleash = 558,
    lead_break = 559,
    unsaddle = 560,
    armor_equip_copper = 561,
    place_item = 563,
    single_swap = 564,
    multi_swap = 565,
    item_enchant_lunge1 = 566,
    item_enchant_lunge2 = 567,
    item_enchant_lunge3 = 568,
    attack_critical = 569,
    item_spear_attack_hit = 570,
    item_spear_attack_miss = 571,
    item_wooden_spear_attack_hit = 572,
    item_wooden_spear_attack_miss = 573,
    imitate_parched = 574,
    imitate_camel_husk = 575,
    item_spear_use = 576,
    item_wooden_spear_use = 577,
    saddle_in_water = 578,
    item_stone_spear_attack_hit = 579,
    item_iron_spear_attack_hit = 580,
    item_copper_spear_attack_hit = 581,
    item_golden_spear_attack_hit = 582,
    item_diamond_spear_attack_hit = 583,
    item_netherite_spear_attack_hit = 584,
    item_stone_spear_attack_miss = 585,
    item_iron_spear_attack_miss = 586,
    item_copper_spear_attack_miss = 587,
    item_golden_spear_attack_miss = 588,
    item_diamond_spear_attack_miss = 589,
    item_netherite_spear_attack_miss = 590,
    item_stone_spear_use = 591,
    item_iron_spear_use = 592,
    item_copper_spear_use = 593,
    item_golden_spear_use = 594,
    item_diamond_spear_use = 595,
    item_netherite_spear_use = 596,
    pause_growth = 597,
    reset_growth = 598,
    pushed_by_player = 599,
    bounce = 600,
    slime_landing = 601,
    absorb_block = 602,
    eject_block = 603,
    geyser_eruption_start = 604,
    geyser_eruption_active = 605,
    record_bounce = 606,
    bucket_fill_land_animal = 607,
    bucket_empty_land_animal = 608,
    geyser_continuous_eruption_start = 609,
    geyser_continuous_eruption_active = 610,
    mount = 611,
    dismount = 612,
    straw_bed_break_leave = 613,
    undefined = 614,
}

public static class LevelSoundEventExtensions {
    public static string ToProtoString(this LevelSoundEvent value) => value.ToProtocolString();

    public static string ToProtocolString(this LevelSoundEvent value) {
        return value switch {
            LevelSoundEvent.item_use_on => "item.use.on",
            LevelSoundEvent.hit => "hit",
            LevelSoundEvent.step => "step",
            LevelSoundEvent.step_baby => "step.baby",
            LevelSoundEvent.fly => "fly",
            LevelSoundEvent.jump => "jump",
            LevelSoundEvent.jump_prevent => "jump.prevent",
            LevelSoundEvent.Break => "break",
            LevelSoundEvent.place => "place",
            LevelSoundEvent.heavy_step => "heavy.step",
            LevelSoundEvent.gallop => "gallop",
            LevelSoundEvent.fall => "fall",
            LevelSoundEvent.hurt => "hurt",
            LevelSoundEvent.hurt_baby => "hurt.baby",
            LevelSoundEvent.hurt_in_water => "hurt.in.water",
            LevelSoundEvent.death => "death",
            LevelSoundEvent.death_baby => "death.baby",
            LevelSoundEvent.death_in_water => "death.in.water",
            LevelSoundEvent.death_to_zombie => "death.to.zombie",
            LevelSoundEvent.ambient => "ambient",
            LevelSoundEvent.ambient_baby => "ambient.baby",
            LevelSoundEvent.ambient_in_water => "ambient.in.water",
            LevelSoundEvent.ambient_in_air => "ambient.in.air",
            LevelSoundEvent.ambient_tame => "ambient.tame",
            LevelSoundEvent.ambient_pollinate => "ambient.pollinate",
            LevelSoundEvent.breathe => "breathe",
            LevelSoundEvent.mad => "mad",
            LevelSoundEvent.boost => "boost",
            LevelSoundEvent.bow => "bow",
            LevelSoundEvent.squish_big => "squish.big",
            LevelSoundEvent.squish_small => "squish.small",
            LevelSoundEvent.fall_big => "fall.big",
            LevelSoundEvent.fall_small => "fall.small",
            LevelSoundEvent.splash => "splash",
            LevelSoundEvent.fizz => "fizz",
            LevelSoundEvent.flap => "flap",
            LevelSoundEvent.swim => "swim",
            LevelSoundEvent.drink => "drink",
            LevelSoundEvent.drink_honey => "drink.honey",
            LevelSoundEvent.drink_milk => "drink.milk",
            LevelSoundEvent.eat => "eat",
            LevelSoundEvent.takeoff => "takeoff",
            LevelSoundEvent.shake => "shake",
            LevelSoundEvent.plop => "plop",
            LevelSoundEvent.land => "land",
            LevelSoundEvent.saddle => "saddle",
            LevelSoundEvent.armor => "armor",
            LevelSoundEvent.mob_armor_stand_place => "mob.armor_stand.place",
            LevelSoundEvent.add_chest => "add.chest",
            LevelSoundEvent.Throw => "throw",
            LevelSoundEvent.attack => "attack",
            LevelSoundEvent.attack_nodamage => "attack.nodamage",
            LevelSoundEvent.attack_strong => "attack.strong",
            LevelSoundEvent.warn => "warn",
            LevelSoundEvent.shear => "shear",
            LevelSoundEvent.milk => "milk",
            LevelSoundEvent.thunder => "thunder",
            LevelSoundEvent.explode => "explode",
            LevelSoundEvent.fire => "fire",
            LevelSoundEvent.ignite => "ignite",
            LevelSoundEvent.fuse => "fuse",
            LevelSoundEvent.stare => "stare",
            LevelSoundEvent.spawn => "spawn",
            LevelSoundEvent.born => "born",
            LevelSoundEvent.shoot => "shoot",
            LevelSoundEvent.break_block => "break.block",
            LevelSoundEvent.launch => "launch",
            LevelSoundEvent.blast => "blast",
            LevelSoundEvent.large_blast => "large.blast",
            LevelSoundEvent.twinkle => "twinkle",
            LevelSoundEvent.remedy => "remedy",
            LevelSoundEvent.unfect => "unfect",
            LevelSoundEvent.convert_to_drowned => "convert_to_drowned",
            LevelSoundEvent.levelup => "levelup",
            LevelSoundEvent.bow_hit => "bow.hit",
            LevelSoundEvent.bullet_hit => "bullet.hit",
            LevelSoundEvent.extinguish_fire => "extinguish.fire",
            LevelSoundEvent.item_fizz => "item.fizz",
            LevelSoundEvent.chest_open => "chest.open",
            LevelSoundEvent.chest_closed => "chest.closed",
            LevelSoundEvent.shulkerbox_open => "shulkerbox.open",
            LevelSoundEvent.shulkerbox_closed => "shulkerbox.closed",
            LevelSoundEvent.enderchest_open => "enderchest.open",
            LevelSoundEvent.enderchest_closed => "enderchest.closed",
            LevelSoundEvent.power_on => "power.on",
            LevelSoundEvent.power_off => "power.off",
            LevelSoundEvent.attach => "attach",
            LevelSoundEvent.detach => "detach",
            LevelSoundEvent.deny => "deny",
            LevelSoundEvent.tripod => "tripod",
            LevelSoundEvent.pop => "pop",
            LevelSoundEvent.drop_slot => "drop.slot",
            LevelSoundEvent.note => "note",
            LevelSoundEvent.thorns => "thorns",
            LevelSoundEvent.piston_in => "piston.in",
            LevelSoundEvent.piston_out => "piston.out",
            LevelSoundEvent.portal => "portal",
            LevelSoundEvent.water => "water",
            LevelSoundEvent.lava_pop => "lava.pop",
            LevelSoundEvent.lava => "lava",
            LevelSoundEvent.beacon_activate => "beacon.activate",
            LevelSoundEvent.beacon_ambient => "beacon.ambient",
            LevelSoundEvent.beacon_deactivate => "beacon.deactivate",
            LevelSoundEvent.beacon_power => "beacon.power",
            LevelSoundEvent.conduit_activate => "conduit.activate",
            LevelSoundEvent.conduit_ambient => "conduit.ambient",
            LevelSoundEvent.conduit_attack => "conduit.attack",
            LevelSoundEvent.conduit_deactivate => "conduit.deactivate",
            LevelSoundEvent.conduit_short => "conduit.short",
            LevelSoundEvent.bubble_pop => "bubble.pop",
            LevelSoundEvent.bubble_up => "bubble.up",
            LevelSoundEvent.bubble_upinside => "bubble.upinside",
            LevelSoundEvent.bubble_down => "bubble.down",
            LevelSoundEvent.bubble_downinside => "bubble.downinside",
            LevelSoundEvent.burp => "burp",
            LevelSoundEvent.bucket_fill_water => "bucket.fill.water",
            LevelSoundEvent.bucket_empty_water => "bucket.empty.water",
            LevelSoundEvent.bucket_fill_lava => "bucket.fill.lava",
            LevelSoundEvent.bucket_empty_lava => "bucket.empty.lava",
            LevelSoundEvent.bucket_fill_fish => "bucket.fill.fish",
            LevelSoundEvent.bucket_empty_fish => "bucket.empty.fish",
            LevelSoundEvent.armor_equip_chain => "armor.equip_chain",
            LevelSoundEvent.armor_equip_diamond => "armor.equip_diamond",
            LevelSoundEvent.armor_equip_elytra => "armor.equip_elytra",
            LevelSoundEvent.armor_equip_generic => "armor.equip_generic",
            LevelSoundEvent.armor_equip_gold => "armor.equip_gold",
            LevelSoundEvent.armor_equip_iron => "armor.equip_iron",
            LevelSoundEvent.armor_equip_leather => "armor.equip_leather",
            LevelSoundEvent.armor_equip_netherite => "armor.equip_netherite",
            LevelSoundEvent.record_13 => "record.13",
            LevelSoundEvent.record_cat => "record.cat",
            LevelSoundEvent.record_blocks => "record.blocks",
            LevelSoundEvent.record_chirp => "record.chirp",
            LevelSoundEvent.record_creator => "record.creator",
            LevelSoundEvent.record_creator_music_box => "record.creator_music_box",
            LevelSoundEvent.record_far => "record.far",
            LevelSoundEvent.record_mall => "record.mall",
            LevelSoundEvent.record_mellohi => "record.mellohi",
            LevelSoundEvent.record_stal => "record.stal",
            LevelSoundEvent.record_strad => "record.strad",
            LevelSoundEvent.record_ward => "record.ward",
            LevelSoundEvent.record_11 => "record.11",
            LevelSoundEvent.record_wait => "record.wait",
            LevelSoundEvent.record_null => "record.null",
            LevelSoundEvent.record_pigstep => "record.pigstep",
            LevelSoundEvent.record_precipice => "record.precipice",
            LevelSoundEvent.record_relic => "record.relic",
            LevelSoundEvent.record_otherside => "record.otherside",
            LevelSoundEvent.record_5 => "record.5",
            LevelSoundEvent.record_tears => "record.tears",
            LevelSoundEvent.record_lava_chicken => "record.lava_chicken",
            LevelSoundEvent.flop => "flop",
            LevelSoundEvent.elderguardian_curse => "elderguardian.curse",
            LevelSoundEvent.teleport => "teleport",
            LevelSoundEvent.shulker_open => "shulker.open",
            LevelSoundEvent.shulker_close => "shulker.close",
            LevelSoundEvent.mob_warning => "mob.warning",
            LevelSoundEvent.mob_warning_baby => "mob.warning.baby",
            LevelSoundEvent.haggle => "haggle",
            LevelSoundEvent.haggle_yes => "haggle.yes",
            LevelSoundEvent.haggle_no => "haggle.no",
            LevelSoundEvent.haggle_idle => "haggle.idle",
            LevelSoundEvent.disappeared => "disappeared",
            LevelSoundEvent.reappeared => "reappeared",
            LevelSoundEvent.chorusgrow => "chorusgrow",
            LevelSoundEvent.chorusdeath => "chorusdeath",
            LevelSoundEvent.glass => "glass",
            LevelSoundEvent.potion_brewed => "potion.brewed",
            LevelSoundEvent.cast_spell => "cast.spell",
            LevelSoundEvent.prepare_attack => "prepare.attack",
            LevelSoundEvent.prepare_summon => "prepare.summon",
            LevelSoundEvent.prepare_wololo => "prepare.wololo",
            LevelSoundEvent.fang => "fang",
            LevelSoundEvent.charge => "charge",
            LevelSoundEvent.camera_take_picture => "camera.take_picture",
            LevelSoundEvent.leashknot_break => "leashknot.break",
            LevelSoundEvent.leashknot_place => "leashknot.place",
            LevelSoundEvent.growl => "growl",
            LevelSoundEvent.whine => "whine",
            LevelSoundEvent.pant => "pant",
            LevelSoundEvent.purr => "purr",
            LevelSoundEvent.purreow => "purreow",
            LevelSoundEvent.death_min_volume => "death.min.volume",
            LevelSoundEvent.death_mid_volume => "death.mid.volume",
            LevelSoundEvent.imitate_blaze => "imitate.blaze",
            LevelSoundEvent.imitate_cave_spider => "imitate.cave_spider",
            LevelSoundEvent.imitate_creeper => "imitate.creeper",
            LevelSoundEvent.imitate_elder_guardian => "imitate.elder_guardian",
            LevelSoundEvent.imitate_ender_dragon => "imitate.ender_dragon",
            LevelSoundEvent.imitate_enderman => "imitate.enderman",
            LevelSoundEvent.imitate_endermite => "imitate.endermite",
            LevelSoundEvent.imitate_evocation_illager => "imitate.evocation_illager",
            LevelSoundEvent.imitate_ghast => "imitate.ghast",
            LevelSoundEvent.imitate_husk => "imitate.husk",
            LevelSoundEvent.imitate_magma_cube => "imitate.magma_cube",
            LevelSoundEvent.imitate_polar_bear => "imitate.polar_bear",
            LevelSoundEvent.imitate_shulker => "imitate.shulker",
            LevelSoundEvent.imitate_silverfish => "imitate.silverfish",
            LevelSoundEvent.imitate_skeleton => "imitate.skeleton",
            LevelSoundEvent.imitate_slime => "imitate.slime",
            LevelSoundEvent.imitate_spider => "imitate.spider",
            LevelSoundEvent.imitate_stray => "imitate.stray",
            LevelSoundEvent.imitate_vex => "imitate.vex",
            LevelSoundEvent.imitate_vindication_illager => "imitate.vindication_illager",
            LevelSoundEvent.imitate_witch => "imitate.witch",
            LevelSoundEvent.imitate_wither => "imitate.wither",
            LevelSoundEvent.imitate_wither_skeleton => "imitate.wither_skeleton",
            LevelSoundEvent.imitate_wolf => "imitate.wolf",
            LevelSoundEvent.imitate_zombie => "imitate.zombie",
            LevelSoundEvent.imitate_zombie_pigman => "imitate.zombie_pigman",
            LevelSoundEvent.imitate_zombie_villager => "imitate.zombie_villager",
            LevelSoundEvent.block_end_portal_frame_fill => "block.end_portal_frame.fill",
            LevelSoundEvent.block_end_portal_spawn => "block.end_portal.spawn",
            LevelSoundEvent.random_anvil_use => "random.anvil_use",
            LevelSoundEvent.bottle_dragonbreath => "bottle.dragonbreath",
            LevelSoundEvent.balloonpop => "balloonpop",
            LevelSoundEvent.sparkler_active => "sparkler.active",
            LevelSoundEvent.item_trident_hit => "item.trident.hit",
            LevelSoundEvent.item_trident_hit_ground => "item.trident.hit_ground",
            LevelSoundEvent.item_trident_return => "item.trident.return",
            LevelSoundEvent.item_trident_riptide_1 => "item.trident.riptide_1",
            LevelSoundEvent.item_trident_riptide_2 => "item.trident.riptide_2",
            LevelSoundEvent.item_trident_riptide_3 => "item.trident.riptide_3",
            LevelSoundEvent.item_trident_throw => "item.trident.throw",
            LevelSoundEvent.item_trident_thunder => "item.trident.thunder",
            LevelSoundEvent.block_fletching_table_use => "block.fletching_table.use",
            LevelSoundEvent.elemconstruct_open => "elemconstruct.open",
            LevelSoundEvent.icebomb_hit => "icebomb.hit",
            LevelSoundEvent.lt_reaction_icebomb => "lt.reaction.icebomb",
            LevelSoundEvent.lt_reaction_bleach => "lt.reaction.bleach",
            LevelSoundEvent.lt_reaction_epaste => "lt.reaction.epaste",
            LevelSoundEvent.lt_reaction_epaste2 => "lt.reaction.epaste2",
            LevelSoundEvent.lt_reaction_fertilizer => "lt.reaction.fertilizer",
            LevelSoundEvent.lt_reaction_fireball => "lt.reaction.fireball",
            LevelSoundEvent.lt_reaction_mgsalt => "lt.reaction.mgsalt",
            LevelSoundEvent.lt_reaction_miscfire => "lt.reaction.miscfire",
            LevelSoundEvent.lt_reaction_fire => "lt.reaction.fire",
            LevelSoundEvent.lt_reaction_miscexplosion => "lt.reaction.miscexplosion",
            LevelSoundEvent.lt_reaction_miscmystical => "lt.reaction.miscmystical",
            LevelSoundEvent.lt_reaction_miscmystical2 => "lt.reaction.miscmystical2",
            LevelSoundEvent.lt_reaction_product => "lt.reaction.product",
            LevelSoundEvent.sparkler_use => "sparkler.use",
            LevelSoundEvent.glowstick_use => "glowstick.use",
            LevelSoundEvent.block_turtle_egg_break => "block.turtle_egg.break",
            LevelSoundEvent.block_turtle_egg_crack => "block.turtle_egg.crack",
            LevelSoundEvent.block_turtle_egg_hatch => "block.turtle_egg.hatch",
            LevelSoundEvent.block_turtle_egg_attack => "block.turtle_egg.attack",
            LevelSoundEvent.block_sniffer_egg_crack => "block.sniffer_egg.crack",
            LevelSoundEvent.block_sniffer_egg_hatch => "block.sniffer_egg.hatch",
            LevelSoundEvent.block_frog_spawn_hatch => "block.frog_spawn.hatch",
            LevelSoundEvent.block_frog_spawn_break => "block.frog_spawn.break",
            LevelSoundEvent.swoop => "swoop",
            LevelSoundEvent.presneeze => "presneeze",
            LevelSoundEvent.sneeze => "sneeze",
            LevelSoundEvent.scared => "scared",
            LevelSoundEvent.ambient_aggressive => "ambient.aggressive",
            LevelSoundEvent.ambient_worried => "ambient.worried",
            LevelSoundEvent.cant_breed => "cant_breed",
            LevelSoundEvent.block_scaffolding_climb => "block.scaffolding.climb",
            LevelSoundEvent.block_bamboo_sapling_place => "block.bamboo_sapling.place",
            LevelSoundEvent.crossbow_loading_start => "crossbow.loading.start",
            LevelSoundEvent.crossbow_loading_middle => "crossbow.loading.middle",
            LevelSoundEvent.crossbow_loading_end => "crossbow.loading.end",
            LevelSoundEvent.crossbow_shoot => "crossbow.shoot",
            LevelSoundEvent.crossbow_quick_charge_start => "crossbow.quick_charge.start",
            LevelSoundEvent.crossbow_quick_charge_middle => "crossbow.quick_charge.middle",
            LevelSoundEvent.crossbow_quick_charge_end => "crossbow.quick_charge.end",
            LevelSoundEvent.item_shield_block => "item.shield.block",
            LevelSoundEvent.portal_travel => "portal.travel",
            LevelSoundEvent.item_book_put => "item.book.put",
            LevelSoundEvent.block_grindstone_use => "block.grindstone.use",
            LevelSoundEvent.block_bell_hit => "block.bell.hit",
            LevelSoundEvent.block_campfire_crackle => "block.campfire.crackle",
            LevelSoundEvent.block_sweet_berry_bush_hurt => "block.sweet_berry_bush.hurt",
            LevelSoundEvent.block_sweet_berry_bush_pick => "block.sweet_berry_bush.pick",
            LevelSoundEvent.block_stonecutter_use => "block.stonecutter.use",
            LevelSoundEvent.block_cartography_table_use => "block.cartography_table.use",
            LevelSoundEvent.block_composter_empty => "block.composter.empty",
            LevelSoundEvent.block_composter_fill => "block.composter.fill",
            LevelSoundEvent.block_composter_fill_success => "block.composter.fill_success",
            LevelSoundEvent.block_composter_ready => "block.composter.ready",
            LevelSoundEvent.roar => "roar",
            LevelSoundEvent.stun => "stun",
            LevelSoundEvent.block_barrel_open => "block.barrel.open",
            LevelSoundEvent.block_barrel_close => "block.barrel.close",
            LevelSoundEvent.raid_horn => "raid.horn",
            LevelSoundEvent.ui_stonecutter_take_result => "ui.stonecutter.take_result",
            LevelSoundEvent.ui_cartography_table_take_result => "ui.cartography_table.take_result",
            LevelSoundEvent.ui_loom_take_result => "ui.loom.take_result",
            LevelSoundEvent.block_smoker_smoke => "block.smoker.smoke",
            LevelSoundEvent.block_blastfurnace_fire_crackle => "block.blastfurnace.fire_crackle",
            LevelSoundEvent.block_smithing_table_use => "block.smithing_table.use",
            LevelSoundEvent.block_loom_use => "block.loom.use",
            LevelSoundEvent.ambient_in_raid => "ambient.in.raid",
            LevelSoundEvent.screech => "screech",
            LevelSoundEvent.sleep => "sleep",
            LevelSoundEvent.block_furnace_lit => "block.furnace.lit",
            LevelSoundEvent.convert_mooshroom => "convert_mooshroom",
            LevelSoundEvent.milk_suspiciously => "milk_suspiciously",
            LevelSoundEvent.celebrate => "celebrate",
            LevelSoundEvent.block_beehive_enter => "block.beehive.enter",
            LevelSoundEvent.block_beehive_exit => "block.beehive.exit",
            LevelSoundEvent.block_beehive_shear => "block.beehive.shear",
            LevelSoundEvent.block_beehive_work => "block.beehive.work",
            LevelSoundEvent.block_beehive_drip => "block.beehive.drip",
            LevelSoundEvent.ambient_cave => "ambient.cave",
            LevelSoundEvent.angry => "angry",
            LevelSoundEvent.retreat => "retreat",
            LevelSoundEvent.converted_to_zombified => "converted_to_zombified",
            LevelSoundEvent.step_lava => "step_lava",
            LevelSoundEvent.tempt => "tempt",
            LevelSoundEvent.panic => "panic",
            LevelSoundEvent.admire => "admire",
            LevelSoundEvent.particle_soul_escape_quiet => "particle.soul_escape.quiet",
            LevelSoundEvent.particle_soul_escape_loud => "particle.soul_escape.loud",
            LevelSoundEvent.respawn_anchor_charge => "respawn_anchor.charge",
            LevelSoundEvent.respawn_anchor_deplete => "respawn_anchor.deplete",
            LevelSoundEvent.respawn_anchor_set_spawn => "respawn_anchor.set_spawn",
            LevelSoundEvent.respawn_anchor_ambient => "respawn_anchor.ambient",
            LevelSoundEvent.ambient_crimson_forest_mood => "ambient.crimson_forest.mood",
            LevelSoundEvent.ambient_warped_forest_mood => "ambient.warped_forest.mood",
            LevelSoundEvent.ambient_soulsand_valley_mood => "ambient.soulsand_valley.mood",
            LevelSoundEvent.ambient_nether_wastes_mood => "ambient.nether_wastes.mood",
            LevelSoundEvent.ambient_crimson_forest_additions => "ambient.crimson_forest.additions",
            LevelSoundEvent.ambient_warped_forest_additions => "ambient.warped_forest.additions",
            LevelSoundEvent.ambient_soulsand_valley_additions => "ambient.soulsand_valley.additions",
            LevelSoundEvent.ambient_nether_wastes_additions => "ambient.nether_wastes.additions",
            LevelSoundEvent.ambient_basalt_deltas_additions => "ambient.basalt_deltas.additions",
            LevelSoundEvent.ambient_crimson_forest_loop => "ambient.crimson_forest.loop",
            LevelSoundEvent.ambient_warped_forest_loop => "ambient.warped_forest.loop",
            LevelSoundEvent.ambient_soulsand_valley_loop => "ambient.soulsand_valley.loop",
            LevelSoundEvent.ambient_nether_wastes_loop => "ambient.nether_wastes.loop",
            LevelSoundEvent.ambient_basalt_deltas_loop => "ambient.basalt_deltas.loop",
            LevelSoundEvent.lodestone_compass_link_compass_to_lodestone => "lodestone_compass.link_compass_to_lodestone",
            LevelSoundEvent.ambient_basalt_deltas_mood => "ambient.basalt_deltas.mood",
            LevelSoundEvent.power_on_sculk_sensor => "power.on.sculk_sensor",
            LevelSoundEvent.power_off_sculk_sensor => "power.off.sculk_sensor",
            LevelSoundEvent.smithing_table_use => "smithing_table.use",
            LevelSoundEvent.Default => "default",
            LevelSoundEvent.lay_egg => "lay_egg",
            LevelSoundEvent.lay_spawn => "lay_spawn",
            LevelSoundEvent.bucket_fill_powder_snow => "bucket.fill.powder_snow",
            LevelSoundEvent.bucket_empty_powder_snow => "bucket.empty.powder_snow",
            LevelSoundEvent.cauldron_drip_water_pointed_dripstone => "cauldron_drip.water.pointed_dripstone",
            LevelSoundEvent.cauldron_drip_lava_pointed_dripstone => "cauldron_drip.lava.pointed_dripstone",
            LevelSoundEvent.tilt_down_big_dripleaf => "tilt_down.big_dripleaf",
            LevelSoundEvent.tilt_up_big_dripleaf => "tilt_up.big_dripleaf",
            LevelSoundEvent.drip_water_pointed_dripstone => "drip.water.pointed_dripstone",
            LevelSoundEvent.pick_berries_cave_vines => "pick_berries.cave_vines",
            LevelSoundEvent.drip_lava_pointed_dripstone => "drip.lava.pointed_dripstone",
            LevelSoundEvent.copper_wax_on => "copper.wax.on",
            LevelSoundEvent.copper_wax_off => "copper.wax.off",
            LevelSoundEvent.scrape => "scrape",
            LevelSoundEvent.item_spyglass_use => "item.spyglass.use",
            LevelSoundEvent.item_spyglass_stop_using => "item.spyglass.stop_using",
            LevelSoundEvent.chime_amethyst_block => "chime.amethyst_block",
            LevelSoundEvent.mob_player_hurt_drown => "mob.player.hurt_drown",
            LevelSoundEvent.mob_player_hurt_on_fire => "mob.player.hurt_on_fire",
            LevelSoundEvent.mob_player_hurt_freeze => "mob.player.hurt_freeze",
            LevelSoundEvent.ambient_screamer => "ambient.screamer",
            LevelSoundEvent.hurt_screamer => "hurt.screamer",
            LevelSoundEvent.death_screamer => "death.screamer",
            LevelSoundEvent.milk_screamer => "milk.screamer",
            LevelSoundEvent.jump_to_block => "jump_to_block",
            LevelSoundEvent.pre_ram => "pre_ram",
            LevelSoundEvent.pre_ram_screamer => "pre_ram.screamer",
            LevelSoundEvent.ram_impact => "ram_impact",
            LevelSoundEvent.ram_impact_screamer => "ram_impact.screamer",
            LevelSoundEvent.squid_ink_squirt => "squid.ink_squirt",
            LevelSoundEvent.glow_squid_ink_squirt => "glow_squid.ink_squirt",
            LevelSoundEvent.convert_to_stray => "convert_to_stray",
            LevelSoundEvent.cake_add_candle => "cake.add_candle",
            LevelSoundEvent.extinguish_candle => "extinguish.candle",
            LevelSoundEvent.ambient_candle => "ambient.candle",
            LevelSoundEvent.block_click => "block.click",
            LevelSoundEvent.block_click_fail => "block.click.fail",
            LevelSoundEvent.block_sculk_catalyst_bloom => "block.sculk_catalyst.bloom",
            LevelSoundEvent.block_sculk_shrieker_shriek => "block.sculk_shrieker.shriek",
            LevelSoundEvent.nearby_close => "nearby_close",
            LevelSoundEvent.nearby_closer => "nearby_closer",
            LevelSoundEvent.nearby_closest => "nearby_closest",
            LevelSoundEvent.agitated => "agitated",
            LevelSoundEvent.listening => "listening",
            LevelSoundEvent.heartbeat => "heartbeat",
            LevelSoundEvent.tongue => "tongue",
            LevelSoundEvent.item_given => "item_given",
            LevelSoundEvent.item_taken => "item_taken",
            LevelSoundEvent.item_thrown => "item_thrown",
            LevelSoundEvent.irongolem_crack => "irongolem.crack",
            LevelSoundEvent.irongolem_repair => "irongolem.repair",
            LevelSoundEvent.horn_break => "horn_break",
            LevelSoundEvent.horn_call0 => "horn_call0",
            LevelSoundEvent.horn_call1 => "horn_call1",
            LevelSoundEvent.horn_call2 => "horn_call2",
            LevelSoundEvent.horn_call3 => "horn_call3",
            LevelSoundEvent.horn_call4 => "horn_call4",
            LevelSoundEvent.horn_call5 => "horn_call5",
            LevelSoundEvent.horn_call6 => "horn_call6",
            LevelSoundEvent.horn_call7 => "horn_call7",
            LevelSoundEvent.imitate_warden => "imitate.warden",
            LevelSoundEvent.listening_angry => "listening_angry",
            LevelSoundEvent.sonic_boom => "sonic_boom",
            LevelSoundEvent.sonic_charge => "sonic_charge",
            LevelSoundEvent.convert_to_frog => "convert_to_frog",
            LevelSoundEvent.block_sculk_spread => "block.sculk.spread",
            LevelSoundEvent.charge_sculk => "charge.sculk",
            LevelSoundEvent.block_sculk_sensor_place => "block.sculk_sensor.place",
            LevelSoundEvent.block_sculk_shrieker_place => "block.sculk_shrieker.place",
            LevelSoundEvent.block_enchanting_table_use => "block.enchanting_table.use",
            LevelSoundEvent.bundle_drop_contents => "bundle.drop_contents",
            LevelSoundEvent.bundle_insert => "bundle.insert",
            LevelSoundEvent.bundle_insert_fail => "bundle.insert_fail",
            LevelSoundEvent.bundle_remove_one => "bundle.remove_one",
            LevelSoundEvent.step_sand => "step_sand",
            LevelSoundEvent.dash_ready => "dash_ready",
            LevelSoundEvent.pressure_plate_click_off => "pressure_plate.click_off",
            LevelSoundEvent.pressure_plate_click_on => "pressure_plate.click_on",
            LevelSoundEvent.button_click_off => "button.click_off",
            LevelSoundEvent.button_click_on => "button.click_on",
            LevelSoundEvent.door_open => "door.open",
            LevelSoundEvent.door_close => "door.close",
            LevelSoundEvent.trapdoor_open => "trapdoor.open",
            LevelSoundEvent.trapdoor_close => "trapdoor.close",
            LevelSoundEvent.fence_gate_open => "fence_gate.open",
            LevelSoundEvent.fence_gate_close => "fence_gate.close",
            LevelSoundEvent.insert => "insert",
            LevelSoundEvent.pickup => "pickup",
            LevelSoundEvent.insert_enchanted => "insert_enchanted",
            LevelSoundEvent.pickup_enchanted => "pickup_enchanted",
            LevelSoundEvent.shatter_pot => "shatter_pot",
            LevelSoundEvent.break_pot => "break_pot",
            LevelSoundEvent.brush => "brush",
            LevelSoundEvent.brush_completed => "brush_completed",
            LevelSoundEvent.block_sign_waxed_interact_fail => "block.sign.waxed_interact_fail",
            LevelSoundEvent.note_bass => "note.bass",
            LevelSoundEvent.pumpkin_carve => "pumpkin.carve",
            LevelSoundEvent.mob_husk_convert_to_zombie => "mob.husk.convert_to_zombie",
            LevelSoundEvent.mob_pig_death => "mob.pig.death",
            LevelSoundEvent.mob_hoglin_converted_to_zombified => "mob.hoglin.converted_to_zombified",
            LevelSoundEvent.ambient_underwater_enter => "ambient.underwater.enter",
            LevelSoundEvent.ambient_underwater_exit => "ambient.underwater.exit",
            LevelSoundEvent.bottle_fill => "bottle.fill",
            LevelSoundEvent.bottle_empty => "bottle.empty",
            LevelSoundEvent.block_decorated_pot_insert => "block.decorated_pot.insert",
            LevelSoundEvent.block_decorated_pot_insert_fail => "block.decorated_pot.insert_fail",
            LevelSoundEvent.crafter_craft => "crafter.craft",
            LevelSoundEvent.crafter_fail => "crafter.fail",
            LevelSoundEvent.crafter_disable_slot => "crafter.disable_slot",
            LevelSoundEvent.block_copper_bulb_turn_on => "block.copper_bulb.turn_on",
            LevelSoundEvent.block_copper_bulb_turn_off => "block.copper_bulb.turn_off",
            LevelSoundEvent.breeze_wind_charge_burst => "breeze_wind_charge.burst",
            LevelSoundEvent.imitate_breeze => "imitate.breeze",
            LevelSoundEvent.trial_spawner_open_shutter => "trial_spawner.open_shutter",
            LevelSoundEvent.trial_spawner_detect_player => "trial_spawner.detect_player",
            LevelSoundEvent.trial_spawner_close_shutter => "trial_spawner.close_shutter",
            LevelSoundEvent.trial_spawner_spawn_mob => "trial_spawner.spawn_mob",
            LevelSoundEvent.trial_spawner_eject_item => "trial_spawner.eject_item",
            LevelSoundEvent.trial_spawner_ambient => "trial_spawner.ambient",
            LevelSoundEvent.mob_armadillo_brush => "mob.armadillo.brush",
            LevelSoundEvent.mob_armadillo_scute_drop => "mob.armadillo.scute_drop",
            LevelSoundEvent.armor_equip_wolf => "armor.equip_wolf",
            LevelSoundEvent.armor_unequip_wolf => "armor.unequip_wolf",
            LevelSoundEvent.reflect => "reflect",
            LevelSoundEvent.vault_open_shutter => "vault.open_shutter",
            LevelSoundEvent.vault_close_shutter => "vault.close_shutter",
            LevelSoundEvent.vault_eject_item => "vault.eject_item",
            LevelSoundEvent.vault_insert_item => "vault.insert_item",
            LevelSoundEvent.vault_insert_item_fail => "vault.insert_item_fail",
            LevelSoundEvent.vault_ambient => "vault.ambient",
            LevelSoundEvent.vault_activate => "vault.activate",
            LevelSoundEvent.vault_deactivate => "vault.deactivate",
            LevelSoundEvent.hurt_reduced => "hurt.reduced",
            LevelSoundEvent.wind_charge_burst => "wind_charge.burst",
            LevelSoundEvent.armor_break_wolf => "armor.break_wolf",
            LevelSoundEvent.armor_crack_wolf => "armor.crack_wolf",
            LevelSoundEvent.armor_repair_wolf => "armor.repair_wolf",
            LevelSoundEvent.mace_smash_air => "mace.smash_air",
            LevelSoundEvent.mace_smash_ground => "mace.smash_ground",
            LevelSoundEvent.mace_heavy_smash_ground => "mace.heavy_smash_ground",
            LevelSoundEvent.trial_spawner_charge_activate => "trial_spawner.charge_activate",
            LevelSoundEvent.trial_spawner_ambient_ominous => "trial_spawner.ambient_ominous",
            LevelSoundEvent.apply_effect_bad_omen => "apply_effect.bad_omen",
            LevelSoundEvent.apply_effect_raid_omen => "apply_effect.raid_omen",
            LevelSoundEvent.apply_effect_trial_omen => "apply_effect.trial_omen",
            LevelSoundEvent.ominous_item_spawner_spawn_item => "ominous_item_spawner.spawn_item",
            LevelSoundEvent.ominous_bottle_end_use => "ominous_bottle.end_use",
            LevelSoundEvent.ominous_item_spawner_spawn_item_begin => "ominous_item_spawner.spawn_item_begin",
            LevelSoundEvent.ominous_item_spawner_about_to_spawn_item => "ominous_item_spawner.about_to_spawn_item",
            LevelSoundEvent.imitate_bogged => "imitate.bogged",
            LevelSoundEvent.vault_reject_rewarded_player => "vault.reject_rewarded_player",
            LevelSoundEvent.imitate_drowned => "imitate.drowned",
            LevelSoundEvent.sponge_absorb => "sponge.absorb",
            LevelSoundEvent.imitate_creaking => "imitate.creaking",
            LevelSoundEvent.block_creaking_heart_trail => "block.creaking_heart.trail",
            LevelSoundEvent.creaking_heart_spawn => "creaking_heart_spawn",
            LevelSoundEvent.activate => "activate",
            LevelSoundEvent.deactivate => "deactivate",
            LevelSoundEvent.freeze => "freeze",
            LevelSoundEvent.unfreeze => "unfreeze",
            LevelSoundEvent.open => "open",
            LevelSoundEvent.open_long => "open_long",
            LevelSoundEvent.close => "close",
            LevelSoundEvent.close_long => "close_long",
            LevelSoundEvent.imitate_phantom => "imitate.phantom",
            LevelSoundEvent.imitate_zoglin => "imitate.zoglin",
            LevelSoundEvent.imitate_guardian => "imitate.guardian",
            LevelSoundEvent.imitate_ravager => "imitate.ravager",
            LevelSoundEvent.imitate_pillager => "imitate.pillager",
            LevelSoundEvent.place_in_water => "place_in_water",
            LevelSoundEvent.state_change => "state_change",
            LevelSoundEvent.imitate_happy_ghast => "imitate.happy_ghast",
            LevelSoundEvent.armor_unequip_generic => "armor.unequip_generic",
            LevelSoundEvent.ambient_weather_the_end_light_flash => "ambient.weather.the_end_light_flash",
            LevelSoundEvent.lead_leash => "lead.leash",
            LevelSoundEvent.lead_unleash => "lead.unleash",
            LevelSoundEvent.lead_break => "lead.break",
            LevelSoundEvent.unsaddle => "unsaddle",
            LevelSoundEvent.armor_equip_copper => "armor.equip_copper",
            LevelSoundEvent.place_item => "place_item",
            LevelSoundEvent.single_swap => "single_swap",
            LevelSoundEvent.multi_swap => "multi_swap",
            LevelSoundEvent.item_enchant_lunge1 => "item.enchant.lunge1",
            LevelSoundEvent.item_enchant_lunge2 => "item.enchant.lunge2",
            LevelSoundEvent.item_enchant_lunge3 => "item.enchant.lunge3",
            LevelSoundEvent.attack_critical => "attack.critical",
            LevelSoundEvent.item_spear_attack_hit => "item.spear.attack_hit",
            LevelSoundEvent.item_spear_attack_miss => "item.spear.attack_miss",
            LevelSoundEvent.item_wooden_spear_attack_hit => "item.wooden_spear.attack_hit",
            LevelSoundEvent.item_wooden_spear_attack_miss => "item.wooden_spear.attack_miss",
            LevelSoundEvent.imitate_parched => "imitate.parched",
            LevelSoundEvent.imitate_camel_husk => "imitate.camel_husk",
            LevelSoundEvent.item_spear_use => "item.spear.use",
            LevelSoundEvent.item_wooden_spear_use => "item.wooden_spear.use",
            LevelSoundEvent.saddle_in_water => "saddle_in_water",
            LevelSoundEvent.item_stone_spear_attack_hit => "item.stone_spear.attack_hit",
            LevelSoundEvent.item_iron_spear_attack_hit => "item.iron_spear.attack_hit",
            LevelSoundEvent.item_copper_spear_attack_hit => "item.copper_spear.attack_hit",
            LevelSoundEvent.item_golden_spear_attack_hit => "item.golden_spear.attack_hit",
            LevelSoundEvent.item_diamond_spear_attack_hit => "item.diamond_spear.attack_hit",
            LevelSoundEvent.item_netherite_spear_attack_hit => "item.netherite_spear.attack_hit",
            LevelSoundEvent.item_stone_spear_attack_miss => "item.stone_spear.attack_miss",
            LevelSoundEvent.item_iron_spear_attack_miss => "item.iron_spear.attack_miss",
            LevelSoundEvent.item_copper_spear_attack_miss => "item.copper_spear.attack_miss",
            LevelSoundEvent.item_golden_spear_attack_miss => "item.golden_spear.attack_miss",
            LevelSoundEvent.item_diamond_spear_attack_miss => "item.diamond_spear.attack_miss",
            LevelSoundEvent.item_netherite_spear_attack_miss => "item.netherite_spear.attack_miss",
            LevelSoundEvent.item_stone_spear_use => "item.stone_spear.use",
            LevelSoundEvent.item_iron_spear_use => "item.iron_spear.use",
            LevelSoundEvent.item_copper_spear_use => "item.copper_spear.use",
            LevelSoundEvent.item_golden_spear_use => "item.golden_spear.use",
            LevelSoundEvent.item_diamond_spear_use => "item.diamond_spear.use",
            LevelSoundEvent.item_netherite_spear_use => "item.netherite_spear.use",
            LevelSoundEvent.pause_growth => "pause_growth",
            LevelSoundEvent.reset_growth => "reset_growth",
            LevelSoundEvent.pushed_by_player => "pushed_by_player",
            LevelSoundEvent.bounce => "bounce",
            LevelSoundEvent.slime_landing => "slime_landing",
            LevelSoundEvent.absorb_block => "absorb_block",
            LevelSoundEvent.eject_block => "eject_block",
            LevelSoundEvent.geyser_eruption_start => "geyser_eruption_start",
            LevelSoundEvent.geyser_eruption_active => "geyser_eruption_active",
            LevelSoundEvent.record_bounce => "record.bounce",
            LevelSoundEvent.bucket_fill_land_animal => "bucket.fill.land_animal",
            LevelSoundEvent.bucket_empty_land_animal => "bucket.empty.land_animal",
            LevelSoundEvent.geyser_continuous_eruption_start => "geyser_continuous_eruption_start",
            LevelSoundEvent.geyser_continuous_eruption_active => "geyser_continuous_eruption_active",
            LevelSoundEvent.mount => "mount",
            LevelSoundEvent.dismount => "dismount",
            LevelSoundEvent.straw_bed_break_leave => "straw_bed.break_leave",
            LevelSoundEvent.undefined => "undefined",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown LevelSoundEvent value.")
        };
    }

    public static LevelSoundEvent FromProtocolString(string value) {
        return value switch {
            "item.use.on" => LevelSoundEvent.item_use_on,
            "hit" => LevelSoundEvent.hit,
            "step" => LevelSoundEvent.step,
            "step.baby" => LevelSoundEvent.step_baby,
            "fly" => LevelSoundEvent.fly,
            "jump" => LevelSoundEvent.jump,
            "jump.prevent" => LevelSoundEvent.jump_prevent,
            "break" => LevelSoundEvent.Break,
            "place" => LevelSoundEvent.place,
            "heavy.step" => LevelSoundEvent.heavy_step,
            "gallop" => LevelSoundEvent.gallop,
            "fall" => LevelSoundEvent.fall,
            "hurt" => LevelSoundEvent.hurt,
            "hurt.baby" => LevelSoundEvent.hurt_baby,
            "hurt.in.water" => LevelSoundEvent.hurt_in_water,
            "death" => LevelSoundEvent.death,
            "death.baby" => LevelSoundEvent.death_baby,
            "death.in.water" => LevelSoundEvent.death_in_water,
            "death.to.zombie" => LevelSoundEvent.death_to_zombie,
            "ambient" => LevelSoundEvent.ambient,
            "ambient.baby" => LevelSoundEvent.ambient_baby,
            "ambient.in.water" => LevelSoundEvent.ambient_in_water,
            "ambient.in.air" => LevelSoundEvent.ambient_in_air,
            "ambient.tame" => LevelSoundEvent.ambient_tame,
            "ambient.pollinate" => LevelSoundEvent.ambient_pollinate,
            "breathe" => LevelSoundEvent.breathe,
            "mad" => LevelSoundEvent.mad,
            "boost" => LevelSoundEvent.boost,
            "bow" => LevelSoundEvent.bow,
            "squish.big" => LevelSoundEvent.squish_big,
            "squish.small" => LevelSoundEvent.squish_small,
            "fall.big" => LevelSoundEvent.fall_big,
            "fall.small" => LevelSoundEvent.fall_small,
            "splash" => LevelSoundEvent.splash,
            "fizz" => LevelSoundEvent.fizz,
            "flap" => LevelSoundEvent.flap,
            "swim" => LevelSoundEvent.swim,
            "drink" => LevelSoundEvent.drink,
            "drink.honey" => LevelSoundEvent.drink_honey,
            "drink.milk" => LevelSoundEvent.drink_milk,
            "eat" => LevelSoundEvent.eat,
            "takeoff" => LevelSoundEvent.takeoff,
            "shake" => LevelSoundEvent.shake,
            "plop" => LevelSoundEvent.plop,
            "land" => LevelSoundEvent.land,
            "saddle" => LevelSoundEvent.saddle,
            "armor" => LevelSoundEvent.armor,
            "mob.armor_stand.place" => LevelSoundEvent.mob_armor_stand_place,
            "add.chest" => LevelSoundEvent.add_chest,
            "throw" => LevelSoundEvent.Throw,
            "attack" => LevelSoundEvent.attack,
            "attack.nodamage" => LevelSoundEvent.attack_nodamage,
            "attack.strong" => LevelSoundEvent.attack_strong,
            "warn" => LevelSoundEvent.warn,
            "shear" => LevelSoundEvent.shear,
            "milk" => LevelSoundEvent.milk,
            "thunder" => LevelSoundEvent.thunder,
            "explode" => LevelSoundEvent.explode,
            "fire" => LevelSoundEvent.fire,
            "ignite" => LevelSoundEvent.ignite,
            "fuse" => LevelSoundEvent.fuse,
            "stare" => LevelSoundEvent.stare,
            "spawn" => LevelSoundEvent.spawn,
            "born" => LevelSoundEvent.born,
            "shoot" => LevelSoundEvent.shoot,
            "break.block" => LevelSoundEvent.break_block,
            "launch" => LevelSoundEvent.launch,
            "blast" => LevelSoundEvent.blast,
            "large.blast" => LevelSoundEvent.large_blast,
            "twinkle" => LevelSoundEvent.twinkle,
            "remedy" => LevelSoundEvent.remedy,
            "unfect" => LevelSoundEvent.unfect,
            "convert_to_drowned" => LevelSoundEvent.convert_to_drowned,
            "levelup" => LevelSoundEvent.levelup,
            "bow.hit" => LevelSoundEvent.bow_hit,
            "bullet.hit" => LevelSoundEvent.bullet_hit,
            "extinguish.fire" => LevelSoundEvent.extinguish_fire,
            "item.fizz" => LevelSoundEvent.item_fizz,
            "chest.open" => LevelSoundEvent.chest_open,
            "chest.closed" => LevelSoundEvent.chest_closed,
            "shulkerbox.open" => LevelSoundEvent.shulkerbox_open,
            "shulkerbox.closed" => LevelSoundEvent.shulkerbox_closed,
            "enderchest.open" => LevelSoundEvent.enderchest_open,
            "enderchest.closed" => LevelSoundEvent.enderchest_closed,
            "power.on" => LevelSoundEvent.power_on,
            "power.off" => LevelSoundEvent.power_off,
            "attach" => LevelSoundEvent.attach,
            "detach" => LevelSoundEvent.detach,
            "deny" => LevelSoundEvent.deny,
            "tripod" => LevelSoundEvent.tripod,
            "pop" => LevelSoundEvent.pop,
            "drop.slot" => LevelSoundEvent.drop_slot,
            "note" => LevelSoundEvent.note,
            "thorns" => LevelSoundEvent.thorns,
            "piston.in" => LevelSoundEvent.piston_in,
            "piston.out" => LevelSoundEvent.piston_out,
            "portal" => LevelSoundEvent.portal,
            "water" => LevelSoundEvent.water,
            "lava.pop" => LevelSoundEvent.lava_pop,
            "lava" => LevelSoundEvent.lava,
            "beacon.activate" => LevelSoundEvent.beacon_activate,
            "beacon.ambient" => LevelSoundEvent.beacon_ambient,
            "beacon.deactivate" => LevelSoundEvent.beacon_deactivate,
            "beacon.power" => LevelSoundEvent.beacon_power,
            "conduit.activate" => LevelSoundEvent.conduit_activate,
            "conduit.ambient" => LevelSoundEvent.conduit_ambient,
            "conduit.attack" => LevelSoundEvent.conduit_attack,
            "conduit.deactivate" => LevelSoundEvent.conduit_deactivate,
            "conduit.short" => LevelSoundEvent.conduit_short,
            "bubble.pop" => LevelSoundEvent.bubble_pop,
            "bubble.up" => LevelSoundEvent.bubble_up,
            "bubble.upinside" => LevelSoundEvent.bubble_upinside,
            "bubble.down" => LevelSoundEvent.bubble_down,
            "bubble.downinside" => LevelSoundEvent.bubble_downinside,
            "burp" => LevelSoundEvent.burp,
            "bucket.fill.water" => LevelSoundEvent.bucket_fill_water,
            "bucket.empty.water" => LevelSoundEvent.bucket_empty_water,
            "bucket.fill.lava" => LevelSoundEvent.bucket_fill_lava,
            "bucket.empty.lava" => LevelSoundEvent.bucket_empty_lava,
            "bucket.fill.fish" => LevelSoundEvent.bucket_fill_fish,
            "bucket.empty.fish" => LevelSoundEvent.bucket_empty_fish,
            "armor.equip_chain" => LevelSoundEvent.armor_equip_chain,
            "armor.equip_diamond" => LevelSoundEvent.armor_equip_diamond,
            "armor.equip_elytra" => LevelSoundEvent.armor_equip_elytra,
            "armor.equip_generic" => LevelSoundEvent.armor_equip_generic,
            "armor.equip_gold" => LevelSoundEvent.armor_equip_gold,
            "armor.equip_iron" => LevelSoundEvent.armor_equip_iron,
            "armor.equip_leather" => LevelSoundEvent.armor_equip_leather,
            "armor.equip_netherite" => LevelSoundEvent.armor_equip_netherite,
            "record.13" => LevelSoundEvent.record_13,
            "record.cat" => LevelSoundEvent.record_cat,
            "record.blocks" => LevelSoundEvent.record_blocks,
            "record.chirp" => LevelSoundEvent.record_chirp,
            "record.creator" => LevelSoundEvent.record_creator,
            "record.creator_music_box" => LevelSoundEvent.record_creator_music_box,
            "record.far" => LevelSoundEvent.record_far,
            "record.mall" => LevelSoundEvent.record_mall,
            "record.mellohi" => LevelSoundEvent.record_mellohi,
            "record.stal" => LevelSoundEvent.record_stal,
            "record.strad" => LevelSoundEvent.record_strad,
            "record.ward" => LevelSoundEvent.record_ward,
            "record.11" => LevelSoundEvent.record_11,
            "record.wait" => LevelSoundEvent.record_wait,
            "record.null" => LevelSoundEvent.record_null,
            "record.pigstep" => LevelSoundEvent.record_pigstep,
            "record.precipice" => LevelSoundEvent.record_precipice,
            "record.relic" => LevelSoundEvent.record_relic,
            "record.otherside" => LevelSoundEvent.record_otherside,
            "record.5" => LevelSoundEvent.record_5,
            "record.tears" => LevelSoundEvent.record_tears,
            "record.lava_chicken" => LevelSoundEvent.record_lava_chicken,
            "flop" => LevelSoundEvent.flop,
            "elderguardian.curse" => LevelSoundEvent.elderguardian_curse,
            "teleport" => LevelSoundEvent.teleport,
            "shulker.open" => LevelSoundEvent.shulker_open,
            "shulker.close" => LevelSoundEvent.shulker_close,
            "mob.warning" => LevelSoundEvent.mob_warning,
            "mob.warning.baby" => LevelSoundEvent.mob_warning_baby,
            "haggle" => LevelSoundEvent.haggle,
            "haggle.yes" => LevelSoundEvent.haggle_yes,
            "haggle.no" => LevelSoundEvent.haggle_no,
            "haggle.idle" => LevelSoundEvent.haggle_idle,
            "disappeared" => LevelSoundEvent.disappeared,
            "reappeared" => LevelSoundEvent.reappeared,
            "chorusgrow" => LevelSoundEvent.chorusgrow,
            "chorusdeath" => LevelSoundEvent.chorusdeath,
            "glass" => LevelSoundEvent.glass,
            "potion.brewed" => LevelSoundEvent.potion_brewed,
            "cast.spell" => LevelSoundEvent.cast_spell,
            "prepare.attack" => LevelSoundEvent.prepare_attack,
            "prepare.summon" => LevelSoundEvent.prepare_summon,
            "prepare.wololo" => LevelSoundEvent.prepare_wololo,
            "fang" => LevelSoundEvent.fang,
            "charge" => LevelSoundEvent.charge,
            "camera.take_picture" => LevelSoundEvent.camera_take_picture,
            "leashknot.break" => LevelSoundEvent.leashknot_break,
            "leashknot.place" => LevelSoundEvent.leashknot_place,
            "growl" => LevelSoundEvent.growl,
            "whine" => LevelSoundEvent.whine,
            "pant" => LevelSoundEvent.pant,
            "purr" => LevelSoundEvent.purr,
            "purreow" => LevelSoundEvent.purreow,
            "death.min.volume" => LevelSoundEvent.death_min_volume,
            "death.mid.volume" => LevelSoundEvent.death_mid_volume,
            "imitate.blaze" => LevelSoundEvent.imitate_blaze,
            "imitate.cave_spider" => LevelSoundEvent.imitate_cave_spider,
            "imitate.creeper" => LevelSoundEvent.imitate_creeper,
            "imitate.elder_guardian" => LevelSoundEvent.imitate_elder_guardian,
            "imitate.ender_dragon" => LevelSoundEvent.imitate_ender_dragon,
            "imitate.enderman" => LevelSoundEvent.imitate_enderman,
            "imitate.endermite" => LevelSoundEvent.imitate_endermite,
            "imitate.evocation_illager" => LevelSoundEvent.imitate_evocation_illager,
            "imitate.ghast" => LevelSoundEvent.imitate_ghast,
            "imitate.husk" => LevelSoundEvent.imitate_husk,
            "imitate.magma_cube" => LevelSoundEvent.imitate_magma_cube,
            "imitate.polar_bear" => LevelSoundEvent.imitate_polar_bear,
            "imitate.shulker" => LevelSoundEvent.imitate_shulker,
            "imitate.silverfish" => LevelSoundEvent.imitate_silverfish,
            "imitate.skeleton" => LevelSoundEvent.imitate_skeleton,
            "imitate.slime" => LevelSoundEvent.imitate_slime,
            "imitate.spider" => LevelSoundEvent.imitate_spider,
            "imitate.stray" => LevelSoundEvent.imitate_stray,
            "imitate.vex" => LevelSoundEvent.imitate_vex,
            "imitate.vindication_illager" => LevelSoundEvent.imitate_vindication_illager,
            "imitate.witch" => LevelSoundEvent.imitate_witch,
            "imitate.wither" => LevelSoundEvent.imitate_wither,
            "imitate.wither_skeleton" => LevelSoundEvent.imitate_wither_skeleton,
            "imitate.wolf" => LevelSoundEvent.imitate_wolf,
            "imitate.zombie" => LevelSoundEvent.imitate_zombie,
            "imitate.zombie_pigman" => LevelSoundEvent.imitate_zombie_pigman,
            "imitate.zombie_villager" => LevelSoundEvent.imitate_zombie_villager,
            "block.end_portal_frame.fill" => LevelSoundEvent.block_end_portal_frame_fill,
            "block.end_portal.spawn" => LevelSoundEvent.block_end_portal_spawn,
            "random.anvil_use" => LevelSoundEvent.random_anvil_use,
            "bottle.dragonbreath" => LevelSoundEvent.bottle_dragonbreath,
            "balloonpop" => LevelSoundEvent.balloonpop,
            "sparkler.active" => LevelSoundEvent.sparkler_active,
            "item.trident.hit" => LevelSoundEvent.item_trident_hit,
            "item.trident.hit_ground" => LevelSoundEvent.item_trident_hit_ground,
            "item.trident.return" => LevelSoundEvent.item_trident_return,
            "item.trident.riptide_1" => LevelSoundEvent.item_trident_riptide_1,
            "item.trident.riptide_2" => LevelSoundEvent.item_trident_riptide_2,
            "item.trident.riptide_3" => LevelSoundEvent.item_trident_riptide_3,
            "item.trident.throw" => LevelSoundEvent.item_trident_throw,
            "item.trident.thunder" => LevelSoundEvent.item_trident_thunder,
            "block.fletching_table.use" => LevelSoundEvent.block_fletching_table_use,
            "elemconstruct.open" => LevelSoundEvent.elemconstruct_open,
            "icebomb.hit" => LevelSoundEvent.icebomb_hit,
            "lt.reaction.icebomb" => LevelSoundEvent.lt_reaction_icebomb,
            "lt.reaction.bleach" => LevelSoundEvent.lt_reaction_bleach,
            "lt.reaction.epaste" => LevelSoundEvent.lt_reaction_epaste,
            "lt.reaction.epaste2" => LevelSoundEvent.lt_reaction_epaste2,
            "lt.reaction.fertilizer" => LevelSoundEvent.lt_reaction_fertilizer,
            "lt.reaction.fireball" => LevelSoundEvent.lt_reaction_fireball,
            "lt.reaction.mgsalt" => LevelSoundEvent.lt_reaction_mgsalt,
            "lt.reaction.miscfire" => LevelSoundEvent.lt_reaction_miscfire,
            "lt.reaction.fire" => LevelSoundEvent.lt_reaction_fire,
            "lt.reaction.miscexplosion" => LevelSoundEvent.lt_reaction_miscexplosion,
            "lt.reaction.miscmystical" => LevelSoundEvent.lt_reaction_miscmystical,
            "lt.reaction.miscmystical2" => LevelSoundEvent.lt_reaction_miscmystical2,
            "lt.reaction.product" => LevelSoundEvent.lt_reaction_product,
            "sparkler.use" => LevelSoundEvent.sparkler_use,
            "glowstick.use" => LevelSoundEvent.glowstick_use,
            "block.turtle_egg.break" => LevelSoundEvent.block_turtle_egg_break,
            "block.turtle_egg.crack" => LevelSoundEvent.block_turtle_egg_crack,
            "block.turtle_egg.hatch" => LevelSoundEvent.block_turtle_egg_hatch,
            "block.turtle_egg.attack" => LevelSoundEvent.block_turtle_egg_attack,
            "block.sniffer_egg.crack" => LevelSoundEvent.block_sniffer_egg_crack,
            "block.sniffer_egg.hatch" => LevelSoundEvent.block_sniffer_egg_hatch,
            "block.frog_spawn.hatch" => LevelSoundEvent.block_frog_spawn_hatch,
            "block.frog_spawn.break" => LevelSoundEvent.block_frog_spawn_break,
            "swoop" => LevelSoundEvent.swoop,
            "presneeze" => LevelSoundEvent.presneeze,
            "sneeze" => LevelSoundEvent.sneeze,
            "scared" => LevelSoundEvent.scared,
            "ambient.aggressive" => LevelSoundEvent.ambient_aggressive,
            "ambient.worried" => LevelSoundEvent.ambient_worried,
            "cant_breed" => LevelSoundEvent.cant_breed,
            "block.scaffolding.climb" => LevelSoundEvent.block_scaffolding_climb,
            "block.bamboo_sapling.place" => LevelSoundEvent.block_bamboo_sapling_place,
            "crossbow.loading.start" => LevelSoundEvent.crossbow_loading_start,
            "crossbow.loading.middle" => LevelSoundEvent.crossbow_loading_middle,
            "crossbow.loading.end" => LevelSoundEvent.crossbow_loading_end,
            "crossbow.shoot" => LevelSoundEvent.crossbow_shoot,
            "crossbow.quick_charge.start" => LevelSoundEvent.crossbow_quick_charge_start,
            "crossbow.quick_charge.middle" => LevelSoundEvent.crossbow_quick_charge_middle,
            "crossbow.quick_charge.end" => LevelSoundEvent.crossbow_quick_charge_end,
            "item.shield.block" => LevelSoundEvent.item_shield_block,
            "portal.travel" => LevelSoundEvent.portal_travel,
            "item.book.put" => LevelSoundEvent.item_book_put,
            "block.grindstone.use" => LevelSoundEvent.block_grindstone_use,
            "block.bell.hit" => LevelSoundEvent.block_bell_hit,
            "block.campfire.crackle" => LevelSoundEvent.block_campfire_crackle,
            "block.sweet_berry_bush.hurt" => LevelSoundEvent.block_sweet_berry_bush_hurt,
            "block.sweet_berry_bush.pick" => LevelSoundEvent.block_sweet_berry_bush_pick,
            "block.stonecutter.use" => LevelSoundEvent.block_stonecutter_use,
            "block.cartography_table.use" => LevelSoundEvent.block_cartography_table_use,
            "block.composter.empty" => LevelSoundEvent.block_composter_empty,
            "block.composter.fill" => LevelSoundEvent.block_composter_fill,
            "block.composter.fill_success" => LevelSoundEvent.block_composter_fill_success,
            "block.composter.ready" => LevelSoundEvent.block_composter_ready,
            "roar" => LevelSoundEvent.roar,
            "stun" => LevelSoundEvent.stun,
            "block.barrel.open" => LevelSoundEvent.block_barrel_open,
            "block.barrel.close" => LevelSoundEvent.block_barrel_close,
            "raid.horn" => LevelSoundEvent.raid_horn,
            "ui.stonecutter.take_result" => LevelSoundEvent.ui_stonecutter_take_result,
            "ui.cartography_table.take_result" => LevelSoundEvent.ui_cartography_table_take_result,
            "ui.loom.take_result" => LevelSoundEvent.ui_loom_take_result,
            "block.smoker.smoke" => LevelSoundEvent.block_smoker_smoke,
            "block.blastfurnace.fire_crackle" => LevelSoundEvent.block_blastfurnace_fire_crackle,
            "block.smithing_table.use" => LevelSoundEvent.block_smithing_table_use,
            "block.loom.use" => LevelSoundEvent.block_loom_use,
            "ambient.in.raid" => LevelSoundEvent.ambient_in_raid,
            "screech" => LevelSoundEvent.screech,
            "sleep" => LevelSoundEvent.sleep,
            "block.furnace.lit" => LevelSoundEvent.block_furnace_lit,
            "convert_mooshroom" => LevelSoundEvent.convert_mooshroom,
            "milk_suspiciously" => LevelSoundEvent.milk_suspiciously,
            "celebrate" => LevelSoundEvent.celebrate,
            "block.beehive.enter" => LevelSoundEvent.block_beehive_enter,
            "block.beehive.exit" => LevelSoundEvent.block_beehive_exit,
            "block.beehive.shear" => LevelSoundEvent.block_beehive_shear,
            "block.beehive.work" => LevelSoundEvent.block_beehive_work,
            "block.beehive.drip" => LevelSoundEvent.block_beehive_drip,
            "ambient.cave" => LevelSoundEvent.ambient_cave,
            "angry" => LevelSoundEvent.angry,
            "retreat" => LevelSoundEvent.retreat,
            "converted_to_zombified" => LevelSoundEvent.converted_to_zombified,
            "step_lava" => LevelSoundEvent.step_lava,
            "tempt" => LevelSoundEvent.tempt,
            "panic" => LevelSoundEvent.panic,
            "admire" => LevelSoundEvent.admire,
            "particle.soul_escape.quiet" => LevelSoundEvent.particle_soul_escape_quiet,
            "particle.soul_escape.loud" => LevelSoundEvent.particle_soul_escape_loud,
            "respawn_anchor.charge" => LevelSoundEvent.respawn_anchor_charge,
            "respawn_anchor.deplete" => LevelSoundEvent.respawn_anchor_deplete,
            "respawn_anchor.set_spawn" => LevelSoundEvent.respawn_anchor_set_spawn,
            "respawn_anchor.ambient" => LevelSoundEvent.respawn_anchor_ambient,
            "ambient.crimson_forest.mood" => LevelSoundEvent.ambient_crimson_forest_mood,
            "ambient.warped_forest.mood" => LevelSoundEvent.ambient_warped_forest_mood,
            "ambient.soulsand_valley.mood" => LevelSoundEvent.ambient_soulsand_valley_mood,
            "ambient.nether_wastes.mood" => LevelSoundEvent.ambient_nether_wastes_mood,
            "ambient.crimson_forest.additions" => LevelSoundEvent.ambient_crimson_forest_additions,
            "ambient.warped_forest.additions" => LevelSoundEvent.ambient_warped_forest_additions,
            "ambient.soulsand_valley.additions" => LevelSoundEvent.ambient_soulsand_valley_additions,
            "ambient.nether_wastes.additions" => LevelSoundEvent.ambient_nether_wastes_additions,
            "ambient.basalt_deltas.additions" => LevelSoundEvent.ambient_basalt_deltas_additions,
            "ambient.crimson_forest.loop" => LevelSoundEvent.ambient_crimson_forest_loop,
            "ambient.warped_forest.loop" => LevelSoundEvent.ambient_warped_forest_loop,
            "ambient.soulsand_valley.loop" => LevelSoundEvent.ambient_soulsand_valley_loop,
            "ambient.nether_wastes.loop" => LevelSoundEvent.ambient_nether_wastes_loop,
            "ambient.basalt_deltas.loop" => LevelSoundEvent.ambient_basalt_deltas_loop,
            "lodestone_compass.link_compass_to_lodestone" => LevelSoundEvent.lodestone_compass_link_compass_to_lodestone,
            "ambient.basalt_deltas.mood" => LevelSoundEvent.ambient_basalt_deltas_mood,
            "power.on.sculk_sensor" => LevelSoundEvent.power_on_sculk_sensor,
            "power.off.sculk_sensor" => LevelSoundEvent.power_off_sculk_sensor,
            "smithing_table.use" => LevelSoundEvent.smithing_table_use,
            "default" => LevelSoundEvent.Default,
            "lay_egg" => LevelSoundEvent.lay_egg,
            "lay_spawn" => LevelSoundEvent.lay_spawn,
            "bucket.fill.powder_snow" => LevelSoundEvent.bucket_fill_powder_snow,
            "bucket.empty.powder_snow" => LevelSoundEvent.bucket_empty_powder_snow,
            "cauldron_drip.water.pointed_dripstone" => LevelSoundEvent.cauldron_drip_water_pointed_dripstone,
            "cauldron_drip.lava.pointed_dripstone" => LevelSoundEvent.cauldron_drip_lava_pointed_dripstone,
            "tilt_down.big_dripleaf" => LevelSoundEvent.tilt_down_big_dripleaf,
            "tilt_up.big_dripleaf" => LevelSoundEvent.tilt_up_big_dripleaf,
            "drip.water.pointed_dripstone" => LevelSoundEvent.drip_water_pointed_dripstone,
            "pick_berries.cave_vines" => LevelSoundEvent.pick_berries_cave_vines,
            "drip.lava.pointed_dripstone" => LevelSoundEvent.drip_lava_pointed_dripstone,
            "copper.wax.on" => LevelSoundEvent.copper_wax_on,
            "copper.wax.off" => LevelSoundEvent.copper_wax_off,
            "scrape" => LevelSoundEvent.scrape,
            "item.spyglass.use" => LevelSoundEvent.item_spyglass_use,
            "item.spyglass.stop_using" => LevelSoundEvent.item_spyglass_stop_using,
            "chime.amethyst_block" => LevelSoundEvent.chime_amethyst_block,
            "mob.player.hurt_drown" => LevelSoundEvent.mob_player_hurt_drown,
            "mob.player.hurt_on_fire" => LevelSoundEvent.mob_player_hurt_on_fire,
            "mob.player.hurt_freeze" => LevelSoundEvent.mob_player_hurt_freeze,
            "ambient.screamer" => LevelSoundEvent.ambient_screamer,
            "hurt.screamer" => LevelSoundEvent.hurt_screamer,
            "death.screamer" => LevelSoundEvent.death_screamer,
            "milk.screamer" => LevelSoundEvent.milk_screamer,
            "jump_to_block" => LevelSoundEvent.jump_to_block,
            "pre_ram" => LevelSoundEvent.pre_ram,
            "pre_ram.screamer" => LevelSoundEvent.pre_ram_screamer,
            "ram_impact" => LevelSoundEvent.ram_impact,
            "ram_impact.screamer" => LevelSoundEvent.ram_impact_screamer,
            "squid.ink_squirt" => LevelSoundEvent.squid_ink_squirt,
            "glow_squid.ink_squirt" => LevelSoundEvent.glow_squid_ink_squirt,
            "convert_to_stray" => LevelSoundEvent.convert_to_stray,
            "cake.add_candle" => LevelSoundEvent.cake_add_candle,
            "extinguish.candle" => LevelSoundEvent.extinguish_candle,
            "ambient.candle" => LevelSoundEvent.ambient_candle,
            "block.click" => LevelSoundEvent.block_click,
            "block.click.fail" => LevelSoundEvent.block_click_fail,
            "block.sculk_catalyst.bloom" => LevelSoundEvent.block_sculk_catalyst_bloom,
            "block.sculk_shrieker.shriek" => LevelSoundEvent.block_sculk_shrieker_shriek,
            "nearby_close" => LevelSoundEvent.nearby_close,
            "nearby_closer" => LevelSoundEvent.nearby_closer,
            "nearby_closest" => LevelSoundEvent.nearby_closest,
            "agitated" => LevelSoundEvent.agitated,
            "listening" => LevelSoundEvent.listening,
            "heartbeat" => LevelSoundEvent.heartbeat,
            "tongue" => LevelSoundEvent.tongue,
            "item_given" => LevelSoundEvent.item_given,
            "item_taken" => LevelSoundEvent.item_taken,
            "item_thrown" => LevelSoundEvent.item_thrown,
            "irongolem.crack" => LevelSoundEvent.irongolem_crack,
            "irongolem.repair" => LevelSoundEvent.irongolem_repair,
            "horn_break" => LevelSoundEvent.horn_break,
            "horn_call0" => LevelSoundEvent.horn_call0,
            "horn_call1" => LevelSoundEvent.horn_call1,
            "horn_call2" => LevelSoundEvent.horn_call2,
            "horn_call3" => LevelSoundEvent.horn_call3,
            "horn_call4" => LevelSoundEvent.horn_call4,
            "horn_call5" => LevelSoundEvent.horn_call5,
            "horn_call6" => LevelSoundEvent.horn_call6,
            "horn_call7" => LevelSoundEvent.horn_call7,
            "imitate.warden" => LevelSoundEvent.imitate_warden,
            "listening_angry" => LevelSoundEvent.listening_angry,
            "sonic_boom" => LevelSoundEvent.sonic_boom,
            "sonic_charge" => LevelSoundEvent.sonic_charge,
            "convert_to_frog" => LevelSoundEvent.convert_to_frog,
            "block.sculk.spread" => LevelSoundEvent.block_sculk_spread,
            "charge.sculk" => LevelSoundEvent.charge_sculk,
            "block.sculk_sensor.place" => LevelSoundEvent.block_sculk_sensor_place,
            "block.sculk_shrieker.place" => LevelSoundEvent.block_sculk_shrieker_place,
            "block.enchanting_table.use" => LevelSoundEvent.block_enchanting_table_use,
            "bundle.drop_contents" => LevelSoundEvent.bundle_drop_contents,
            "bundle.insert" => LevelSoundEvent.bundle_insert,
            "bundle.insert_fail" => LevelSoundEvent.bundle_insert_fail,
            "bundle.remove_one" => LevelSoundEvent.bundle_remove_one,
            "step_sand" => LevelSoundEvent.step_sand,
            "dash_ready" => LevelSoundEvent.dash_ready,
            "pressure_plate.click_off" => LevelSoundEvent.pressure_plate_click_off,
            "pressure_plate.click_on" => LevelSoundEvent.pressure_plate_click_on,
            "button.click_off" => LevelSoundEvent.button_click_off,
            "button.click_on" => LevelSoundEvent.button_click_on,
            "door.open" => LevelSoundEvent.door_open,
            "door.close" => LevelSoundEvent.door_close,
            "trapdoor.open" => LevelSoundEvent.trapdoor_open,
            "trapdoor.close" => LevelSoundEvent.trapdoor_close,
            "fence_gate.open" => LevelSoundEvent.fence_gate_open,
            "fence_gate.close" => LevelSoundEvent.fence_gate_close,
            "insert" => LevelSoundEvent.insert,
            "pickup" => LevelSoundEvent.pickup,
            "insert_enchanted" => LevelSoundEvent.insert_enchanted,
            "pickup_enchanted" => LevelSoundEvent.pickup_enchanted,
            "shatter_pot" => LevelSoundEvent.shatter_pot,
            "break_pot" => LevelSoundEvent.break_pot,
            "brush" => LevelSoundEvent.brush,
            "brush_completed" => LevelSoundEvent.brush_completed,
            "block.sign.waxed_interact_fail" => LevelSoundEvent.block_sign_waxed_interact_fail,
            "note.bass" => LevelSoundEvent.note_bass,
            "pumpkin.carve" => LevelSoundEvent.pumpkin_carve,
            "mob.husk.convert_to_zombie" => LevelSoundEvent.mob_husk_convert_to_zombie,
            "mob.pig.death" => LevelSoundEvent.mob_pig_death,
            "mob.hoglin.converted_to_zombified" => LevelSoundEvent.mob_hoglin_converted_to_zombified,
            "ambient.underwater.enter" => LevelSoundEvent.ambient_underwater_enter,
            "ambient.underwater.exit" => LevelSoundEvent.ambient_underwater_exit,
            "bottle.fill" => LevelSoundEvent.bottle_fill,
            "bottle.empty" => LevelSoundEvent.bottle_empty,
            "block.decorated_pot.insert" => LevelSoundEvent.block_decorated_pot_insert,
            "block.decorated_pot.insert_fail" => LevelSoundEvent.block_decorated_pot_insert_fail,
            "crafter.craft" => LevelSoundEvent.crafter_craft,
            "crafter.fail" => LevelSoundEvent.crafter_fail,
            "crafter.disable_slot" => LevelSoundEvent.crafter_disable_slot,
            "block.copper_bulb.turn_on" => LevelSoundEvent.block_copper_bulb_turn_on,
            "block.copper_bulb.turn_off" => LevelSoundEvent.block_copper_bulb_turn_off,
            "breeze_wind_charge.burst" => LevelSoundEvent.breeze_wind_charge_burst,
            "imitate.breeze" => LevelSoundEvent.imitate_breeze,
            "trial_spawner.open_shutter" => LevelSoundEvent.trial_spawner_open_shutter,
            "trial_spawner.detect_player" => LevelSoundEvent.trial_spawner_detect_player,
            "trial_spawner.close_shutter" => LevelSoundEvent.trial_spawner_close_shutter,
            "trial_spawner.spawn_mob" => LevelSoundEvent.trial_spawner_spawn_mob,
            "trial_spawner.eject_item" => LevelSoundEvent.trial_spawner_eject_item,
            "trial_spawner.ambient" => LevelSoundEvent.trial_spawner_ambient,
            "mob.armadillo.brush" => LevelSoundEvent.mob_armadillo_brush,
            "mob.armadillo.scute_drop" => LevelSoundEvent.mob_armadillo_scute_drop,
            "armor.equip_wolf" => LevelSoundEvent.armor_equip_wolf,
            "armor.unequip_wolf" => LevelSoundEvent.armor_unequip_wolf,
            "reflect" => LevelSoundEvent.reflect,
            "vault.open_shutter" => LevelSoundEvent.vault_open_shutter,
            "vault.close_shutter" => LevelSoundEvent.vault_close_shutter,
            "vault.eject_item" => LevelSoundEvent.vault_eject_item,
            "vault.insert_item" => LevelSoundEvent.vault_insert_item,
            "vault.insert_item_fail" => LevelSoundEvent.vault_insert_item_fail,
            "vault.ambient" => LevelSoundEvent.vault_ambient,
            "vault.activate" => LevelSoundEvent.vault_activate,
            "vault.deactivate" => LevelSoundEvent.vault_deactivate,
            "hurt.reduced" => LevelSoundEvent.hurt_reduced,
            "wind_charge.burst" => LevelSoundEvent.wind_charge_burst,
            "armor.break_wolf" => LevelSoundEvent.armor_break_wolf,
            "armor.crack_wolf" => LevelSoundEvent.armor_crack_wolf,
            "armor.repair_wolf" => LevelSoundEvent.armor_repair_wolf,
            "mace.smash_air" => LevelSoundEvent.mace_smash_air,
            "mace.smash_ground" => LevelSoundEvent.mace_smash_ground,
            "mace.heavy_smash_ground" => LevelSoundEvent.mace_heavy_smash_ground,
            "trial_spawner.charge_activate" => LevelSoundEvent.trial_spawner_charge_activate,
            "trial_spawner.ambient_ominous" => LevelSoundEvent.trial_spawner_ambient_ominous,
            "apply_effect.bad_omen" => LevelSoundEvent.apply_effect_bad_omen,
            "apply_effect.raid_omen" => LevelSoundEvent.apply_effect_raid_omen,
            "apply_effect.trial_omen" => LevelSoundEvent.apply_effect_trial_omen,
            "ominous_item_spawner.spawn_item" => LevelSoundEvent.ominous_item_spawner_spawn_item,
            "ominous_bottle.end_use" => LevelSoundEvent.ominous_bottle_end_use,
            "ominous_item_spawner.spawn_item_begin" => LevelSoundEvent.ominous_item_spawner_spawn_item_begin,
            "ominous_item_spawner.about_to_spawn_item" => LevelSoundEvent.ominous_item_spawner_about_to_spawn_item,
            "imitate.bogged" => LevelSoundEvent.imitate_bogged,
            "vault.reject_rewarded_player" => LevelSoundEvent.vault_reject_rewarded_player,
            "imitate.drowned" => LevelSoundEvent.imitate_drowned,
            "sponge.absorb" => LevelSoundEvent.sponge_absorb,
            "imitate.creaking" => LevelSoundEvent.imitate_creaking,
            "block.creaking_heart.trail" => LevelSoundEvent.block_creaking_heart_trail,
            "creaking_heart_spawn" => LevelSoundEvent.creaking_heart_spawn,
            "activate" => LevelSoundEvent.activate,
            "deactivate" => LevelSoundEvent.deactivate,
            "freeze" => LevelSoundEvent.freeze,
            "unfreeze" => LevelSoundEvent.unfreeze,
            "open" => LevelSoundEvent.open,
            "open_long" => LevelSoundEvent.open_long,
            "close" => LevelSoundEvent.close,
            "close_long" => LevelSoundEvent.close_long,
            "imitate.phantom" => LevelSoundEvent.imitate_phantom,
            "imitate.zoglin" => LevelSoundEvent.imitate_zoglin,
            "imitate.guardian" => LevelSoundEvent.imitate_guardian,
            "imitate.ravager" => LevelSoundEvent.imitate_ravager,
            "imitate.pillager" => LevelSoundEvent.imitate_pillager,
            "place_in_water" => LevelSoundEvent.place_in_water,
            "state_change" => LevelSoundEvent.state_change,
            "imitate.happy_ghast" => LevelSoundEvent.imitate_happy_ghast,
            "armor.unequip_generic" => LevelSoundEvent.armor_unequip_generic,
            "ambient.weather.the_end_light_flash" => LevelSoundEvent.ambient_weather_the_end_light_flash,
            "lead.leash" => LevelSoundEvent.lead_leash,
            "lead.unleash" => LevelSoundEvent.lead_unleash,
            "lead.break" => LevelSoundEvent.lead_break,
            "unsaddle" => LevelSoundEvent.unsaddle,
            "armor.equip_copper" => LevelSoundEvent.armor_equip_copper,
            "place_item" => LevelSoundEvent.place_item,
            "single_swap" => LevelSoundEvent.single_swap,
            "multi_swap" => LevelSoundEvent.multi_swap,
            "item.enchant.lunge1" => LevelSoundEvent.item_enchant_lunge1,
            "item.enchant.lunge2" => LevelSoundEvent.item_enchant_lunge2,
            "item.enchant.lunge3" => LevelSoundEvent.item_enchant_lunge3,
            "attack.critical" => LevelSoundEvent.attack_critical,
            "item.spear.attack_hit" => LevelSoundEvent.item_spear_attack_hit,
            "item.spear.attack_miss" => LevelSoundEvent.item_spear_attack_miss,
            "item.wooden_spear.attack_hit" => LevelSoundEvent.item_wooden_spear_attack_hit,
            "item.wooden_spear.attack_miss" => LevelSoundEvent.item_wooden_spear_attack_miss,
            "imitate.parched" => LevelSoundEvent.imitate_parched,
            "imitate.camel_husk" => LevelSoundEvent.imitate_camel_husk,
            "item.spear.use" => LevelSoundEvent.item_spear_use,
            "item.wooden_spear.use" => LevelSoundEvent.item_wooden_spear_use,
            "saddle_in_water" => LevelSoundEvent.saddle_in_water,
            "item.stone_spear.attack_hit" => LevelSoundEvent.item_stone_spear_attack_hit,
            "item.iron_spear.attack_hit" => LevelSoundEvent.item_iron_spear_attack_hit,
            "item.copper_spear.attack_hit" => LevelSoundEvent.item_copper_spear_attack_hit,
            "item.golden_spear.attack_hit" => LevelSoundEvent.item_golden_spear_attack_hit,
            "item.diamond_spear.attack_hit" => LevelSoundEvent.item_diamond_spear_attack_hit,
            "item.netherite_spear.attack_hit" => LevelSoundEvent.item_netherite_spear_attack_hit,
            "item.stone_spear.attack_miss" => LevelSoundEvent.item_stone_spear_attack_miss,
            "item.iron_spear.attack_miss" => LevelSoundEvent.item_iron_spear_attack_miss,
            "item.copper_spear.attack_miss" => LevelSoundEvent.item_copper_spear_attack_miss,
            "item.golden_spear.attack_miss" => LevelSoundEvent.item_golden_spear_attack_miss,
            "item.diamond_spear.attack_miss" => LevelSoundEvent.item_diamond_spear_attack_miss,
            "item.netherite_spear.attack_miss" => LevelSoundEvent.item_netherite_spear_attack_miss,
            "item.stone_spear.use" => LevelSoundEvent.item_stone_spear_use,
            "item.iron_spear.use" => LevelSoundEvent.item_iron_spear_use,
            "item.copper_spear.use" => LevelSoundEvent.item_copper_spear_use,
            "item.golden_spear.use" => LevelSoundEvent.item_golden_spear_use,
            "item.diamond_spear.use" => LevelSoundEvent.item_diamond_spear_use,
            "item.netherite_spear.use" => LevelSoundEvent.item_netherite_spear_use,
            "pause_growth" => LevelSoundEvent.pause_growth,
            "reset_growth" => LevelSoundEvent.reset_growth,
            "pushed_by_player" => LevelSoundEvent.pushed_by_player,
            "bounce" => LevelSoundEvent.bounce,
            "slime_landing" => LevelSoundEvent.slime_landing,
            "absorb_block" => LevelSoundEvent.absorb_block,
            "eject_block" => LevelSoundEvent.eject_block,
            "geyser_eruption_start" => LevelSoundEvent.geyser_eruption_start,
            "geyser_eruption_active" => LevelSoundEvent.geyser_eruption_active,
            "record.bounce" => LevelSoundEvent.record_bounce,
            "bucket.fill.land_animal" => LevelSoundEvent.bucket_fill_land_animal,
            "bucket.empty.land_animal" => LevelSoundEvent.bucket_empty_land_animal,
            "geyser_continuous_eruption_start" => LevelSoundEvent.geyser_continuous_eruption_start,
            "geyser_continuous_eruption_active" => LevelSoundEvent.geyser_continuous_eruption_active,
            "mount" => LevelSoundEvent.mount,
            "dismount" => LevelSoundEvent.dismount,
            "straw_bed.break_leave" => LevelSoundEvent.straw_bed_break_leave,
            "undefined" => LevelSoundEvent.undefined,
            _ => throw new ArgumentException($"Unknown LevelSoundEvent protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out LevelSoundEvent result) {
        switch (value) {
            case "item.use.on":
                result = LevelSoundEvent.item_use_on;
                return true;
            case "hit":
                result = LevelSoundEvent.hit;
                return true;
            case "step":
                result = LevelSoundEvent.step;
                return true;
            case "step.baby":
                result = LevelSoundEvent.step_baby;
                return true;
            case "fly":
                result = LevelSoundEvent.fly;
                return true;
            case "jump":
                result = LevelSoundEvent.jump;
                return true;
            case "jump.prevent":
                result = LevelSoundEvent.jump_prevent;
                return true;
            case "break":
                result = LevelSoundEvent.Break;
                return true;
            case "place":
                result = LevelSoundEvent.place;
                return true;
            case "heavy.step":
                result = LevelSoundEvent.heavy_step;
                return true;
            case "gallop":
                result = LevelSoundEvent.gallop;
                return true;
            case "fall":
                result = LevelSoundEvent.fall;
                return true;
            case "hurt":
                result = LevelSoundEvent.hurt;
                return true;
            case "hurt.baby":
                result = LevelSoundEvent.hurt_baby;
                return true;
            case "hurt.in.water":
                result = LevelSoundEvent.hurt_in_water;
                return true;
            case "death":
                result = LevelSoundEvent.death;
                return true;
            case "death.baby":
                result = LevelSoundEvent.death_baby;
                return true;
            case "death.in.water":
                result = LevelSoundEvent.death_in_water;
                return true;
            case "death.to.zombie":
                result = LevelSoundEvent.death_to_zombie;
                return true;
            case "ambient":
                result = LevelSoundEvent.ambient;
                return true;
            case "ambient.baby":
                result = LevelSoundEvent.ambient_baby;
                return true;
            case "ambient.in.water":
                result = LevelSoundEvent.ambient_in_water;
                return true;
            case "ambient.in.air":
                result = LevelSoundEvent.ambient_in_air;
                return true;
            case "ambient.tame":
                result = LevelSoundEvent.ambient_tame;
                return true;
            case "ambient.pollinate":
                result = LevelSoundEvent.ambient_pollinate;
                return true;
            case "breathe":
                result = LevelSoundEvent.breathe;
                return true;
            case "mad":
                result = LevelSoundEvent.mad;
                return true;
            case "boost":
                result = LevelSoundEvent.boost;
                return true;
            case "bow":
                result = LevelSoundEvent.bow;
                return true;
            case "squish.big":
                result = LevelSoundEvent.squish_big;
                return true;
            case "squish.small":
                result = LevelSoundEvent.squish_small;
                return true;
            case "fall.big":
                result = LevelSoundEvent.fall_big;
                return true;
            case "fall.small":
                result = LevelSoundEvent.fall_small;
                return true;
            case "splash":
                result = LevelSoundEvent.splash;
                return true;
            case "fizz":
                result = LevelSoundEvent.fizz;
                return true;
            case "flap":
                result = LevelSoundEvent.flap;
                return true;
            case "swim":
                result = LevelSoundEvent.swim;
                return true;
            case "drink":
                result = LevelSoundEvent.drink;
                return true;
            case "drink.honey":
                result = LevelSoundEvent.drink_honey;
                return true;
            case "drink.milk":
                result = LevelSoundEvent.drink_milk;
                return true;
            case "eat":
                result = LevelSoundEvent.eat;
                return true;
            case "takeoff":
                result = LevelSoundEvent.takeoff;
                return true;
            case "shake":
                result = LevelSoundEvent.shake;
                return true;
            case "plop":
                result = LevelSoundEvent.plop;
                return true;
            case "land":
                result = LevelSoundEvent.land;
                return true;
            case "saddle":
                result = LevelSoundEvent.saddle;
                return true;
            case "armor":
                result = LevelSoundEvent.armor;
                return true;
            case "mob.armor_stand.place":
                result = LevelSoundEvent.mob_armor_stand_place;
                return true;
            case "add.chest":
                result = LevelSoundEvent.add_chest;
                return true;
            case "throw":
                result = LevelSoundEvent.Throw;
                return true;
            case "attack":
                result = LevelSoundEvent.attack;
                return true;
            case "attack.nodamage":
                result = LevelSoundEvent.attack_nodamage;
                return true;
            case "attack.strong":
                result = LevelSoundEvent.attack_strong;
                return true;
            case "warn":
                result = LevelSoundEvent.warn;
                return true;
            case "shear":
                result = LevelSoundEvent.shear;
                return true;
            case "milk":
                result = LevelSoundEvent.milk;
                return true;
            case "thunder":
                result = LevelSoundEvent.thunder;
                return true;
            case "explode":
                result = LevelSoundEvent.explode;
                return true;
            case "fire":
                result = LevelSoundEvent.fire;
                return true;
            case "ignite":
                result = LevelSoundEvent.ignite;
                return true;
            case "fuse":
                result = LevelSoundEvent.fuse;
                return true;
            case "stare":
                result = LevelSoundEvent.stare;
                return true;
            case "spawn":
                result = LevelSoundEvent.spawn;
                return true;
            case "born":
                result = LevelSoundEvent.born;
                return true;
            case "shoot":
                result = LevelSoundEvent.shoot;
                return true;
            case "break.block":
                result = LevelSoundEvent.break_block;
                return true;
            case "launch":
                result = LevelSoundEvent.launch;
                return true;
            case "blast":
                result = LevelSoundEvent.blast;
                return true;
            case "large.blast":
                result = LevelSoundEvent.large_blast;
                return true;
            case "twinkle":
                result = LevelSoundEvent.twinkle;
                return true;
            case "remedy":
                result = LevelSoundEvent.remedy;
                return true;
            case "unfect":
                result = LevelSoundEvent.unfect;
                return true;
            case "convert_to_drowned":
                result = LevelSoundEvent.convert_to_drowned;
                return true;
            case "levelup":
                result = LevelSoundEvent.levelup;
                return true;
            case "bow.hit":
                result = LevelSoundEvent.bow_hit;
                return true;
            case "bullet.hit":
                result = LevelSoundEvent.bullet_hit;
                return true;
            case "extinguish.fire":
                result = LevelSoundEvent.extinguish_fire;
                return true;
            case "item.fizz":
                result = LevelSoundEvent.item_fizz;
                return true;
            case "chest.open":
                result = LevelSoundEvent.chest_open;
                return true;
            case "chest.closed":
                result = LevelSoundEvent.chest_closed;
                return true;
            case "shulkerbox.open":
                result = LevelSoundEvent.shulkerbox_open;
                return true;
            case "shulkerbox.closed":
                result = LevelSoundEvent.shulkerbox_closed;
                return true;
            case "enderchest.open":
                result = LevelSoundEvent.enderchest_open;
                return true;
            case "enderchest.closed":
                result = LevelSoundEvent.enderchest_closed;
                return true;
            case "power.on":
                result = LevelSoundEvent.power_on;
                return true;
            case "power.off":
                result = LevelSoundEvent.power_off;
                return true;
            case "attach":
                result = LevelSoundEvent.attach;
                return true;
            case "detach":
                result = LevelSoundEvent.detach;
                return true;
            case "deny":
                result = LevelSoundEvent.deny;
                return true;
            case "tripod":
                result = LevelSoundEvent.tripod;
                return true;
            case "pop":
                result = LevelSoundEvent.pop;
                return true;
            case "drop.slot":
                result = LevelSoundEvent.drop_slot;
                return true;
            case "note":
                result = LevelSoundEvent.note;
                return true;
            case "thorns":
                result = LevelSoundEvent.thorns;
                return true;
            case "piston.in":
                result = LevelSoundEvent.piston_in;
                return true;
            case "piston.out":
                result = LevelSoundEvent.piston_out;
                return true;
            case "portal":
                result = LevelSoundEvent.portal;
                return true;
            case "water":
                result = LevelSoundEvent.water;
                return true;
            case "lava.pop":
                result = LevelSoundEvent.lava_pop;
                return true;
            case "lava":
                result = LevelSoundEvent.lava;
                return true;
            case "beacon.activate":
                result = LevelSoundEvent.beacon_activate;
                return true;
            case "beacon.ambient":
                result = LevelSoundEvent.beacon_ambient;
                return true;
            case "beacon.deactivate":
                result = LevelSoundEvent.beacon_deactivate;
                return true;
            case "beacon.power":
                result = LevelSoundEvent.beacon_power;
                return true;
            case "conduit.activate":
                result = LevelSoundEvent.conduit_activate;
                return true;
            case "conduit.ambient":
                result = LevelSoundEvent.conduit_ambient;
                return true;
            case "conduit.attack":
                result = LevelSoundEvent.conduit_attack;
                return true;
            case "conduit.deactivate":
                result = LevelSoundEvent.conduit_deactivate;
                return true;
            case "conduit.short":
                result = LevelSoundEvent.conduit_short;
                return true;
            case "bubble.pop":
                result = LevelSoundEvent.bubble_pop;
                return true;
            case "bubble.up":
                result = LevelSoundEvent.bubble_up;
                return true;
            case "bubble.upinside":
                result = LevelSoundEvent.bubble_upinside;
                return true;
            case "bubble.down":
                result = LevelSoundEvent.bubble_down;
                return true;
            case "bubble.downinside":
                result = LevelSoundEvent.bubble_downinside;
                return true;
            case "burp":
                result = LevelSoundEvent.burp;
                return true;
            case "bucket.fill.water":
                result = LevelSoundEvent.bucket_fill_water;
                return true;
            case "bucket.empty.water":
                result = LevelSoundEvent.bucket_empty_water;
                return true;
            case "bucket.fill.lava":
                result = LevelSoundEvent.bucket_fill_lava;
                return true;
            case "bucket.empty.lava":
                result = LevelSoundEvent.bucket_empty_lava;
                return true;
            case "bucket.fill.fish":
                result = LevelSoundEvent.bucket_fill_fish;
                return true;
            case "bucket.empty.fish":
                result = LevelSoundEvent.bucket_empty_fish;
                return true;
            case "armor.equip_chain":
                result = LevelSoundEvent.armor_equip_chain;
                return true;
            case "armor.equip_diamond":
                result = LevelSoundEvent.armor_equip_diamond;
                return true;
            case "armor.equip_elytra":
                result = LevelSoundEvent.armor_equip_elytra;
                return true;
            case "armor.equip_generic":
                result = LevelSoundEvent.armor_equip_generic;
                return true;
            case "armor.equip_gold":
                result = LevelSoundEvent.armor_equip_gold;
                return true;
            case "armor.equip_iron":
                result = LevelSoundEvent.armor_equip_iron;
                return true;
            case "armor.equip_leather":
                result = LevelSoundEvent.armor_equip_leather;
                return true;
            case "armor.equip_netherite":
                result = LevelSoundEvent.armor_equip_netherite;
                return true;
            case "record.13":
                result = LevelSoundEvent.record_13;
                return true;
            case "record.cat":
                result = LevelSoundEvent.record_cat;
                return true;
            case "record.blocks":
                result = LevelSoundEvent.record_blocks;
                return true;
            case "record.chirp":
                result = LevelSoundEvent.record_chirp;
                return true;
            case "record.creator":
                result = LevelSoundEvent.record_creator;
                return true;
            case "record.creator_music_box":
                result = LevelSoundEvent.record_creator_music_box;
                return true;
            case "record.far":
                result = LevelSoundEvent.record_far;
                return true;
            case "record.mall":
                result = LevelSoundEvent.record_mall;
                return true;
            case "record.mellohi":
                result = LevelSoundEvent.record_mellohi;
                return true;
            case "record.stal":
                result = LevelSoundEvent.record_stal;
                return true;
            case "record.strad":
                result = LevelSoundEvent.record_strad;
                return true;
            case "record.ward":
                result = LevelSoundEvent.record_ward;
                return true;
            case "record.11":
                result = LevelSoundEvent.record_11;
                return true;
            case "record.wait":
                result = LevelSoundEvent.record_wait;
                return true;
            case "record.null":
                result = LevelSoundEvent.record_null;
                return true;
            case "record.pigstep":
                result = LevelSoundEvent.record_pigstep;
                return true;
            case "record.precipice":
                result = LevelSoundEvent.record_precipice;
                return true;
            case "record.relic":
                result = LevelSoundEvent.record_relic;
                return true;
            case "record.otherside":
                result = LevelSoundEvent.record_otherside;
                return true;
            case "record.5":
                result = LevelSoundEvent.record_5;
                return true;
            case "record.tears":
                result = LevelSoundEvent.record_tears;
                return true;
            case "record.lava_chicken":
                result = LevelSoundEvent.record_lava_chicken;
                return true;
            case "flop":
                result = LevelSoundEvent.flop;
                return true;
            case "elderguardian.curse":
                result = LevelSoundEvent.elderguardian_curse;
                return true;
            case "teleport":
                result = LevelSoundEvent.teleport;
                return true;
            case "shulker.open":
                result = LevelSoundEvent.shulker_open;
                return true;
            case "shulker.close":
                result = LevelSoundEvent.shulker_close;
                return true;
            case "mob.warning":
                result = LevelSoundEvent.mob_warning;
                return true;
            case "mob.warning.baby":
                result = LevelSoundEvent.mob_warning_baby;
                return true;
            case "haggle":
                result = LevelSoundEvent.haggle;
                return true;
            case "haggle.yes":
                result = LevelSoundEvent.haggle_yes;
                return true;
            case "haggle.no":
                result = LevelSoundEvent.haggle_no;
                return true;
            case "haggle.idle":
                result = LevelSoundEvent.haggle_idle;
                return true;
            case "disappeared":
                result = LevelSoundEvent.disappeared;
                return true;
            case "reappeared":
                result = LevelSoundEvent.reappeared;
                return true;
            case "chorusgrow":
                result = LevelSoundEvent.chorusgrow;
                return true;
            case "chorusdeath":
                result = LevelSoundEvent.chorusdeath;
                return true;
            case "glass":
                result = LevelSoundEvent.glass;
                return true;
            case "potion.brewed":
                result = LevelSoundEvent.potion_brewed;
                return true;
            case "cast.spell":
                result = LevelSoundEvent.cast_spell;
                return true;
            case "prepare.attack":
                result = LevelSoundEvent.prepare_attack;
                return true;
            case "prepare.summon":
                result = LevelSoundEvent.prepare_summon;
                return true;
            case "prepare.wololo":
                result = LevelSoundEvent.prepare_wololo;
                return true;
            case "fang":
                result = LevelSoundEvent.fang;
                return true;
            case "charge":
                result = LevelSoundEvent.charge;
                return true;
            case "camera.take_picture":
                result = LevelSoundEvent.camera_take_picture;
                return true;
            case "leashknot.break":
                result = LevelSoundEvent.leashknot_break;
                return true;
            case "leashknot.place":
                result = LevelSoundEvent.leashknot_place;
                return true;
            case "growl":
                result = LevelSoundEvent.growl;
                return true;
            case "whine":
                result = LevelSoundEvent.whine;
                return true;
            case "pant":
                result = LevelSoundEvent.pant;
                return true;
            case "purr":
                result = LevelSoundEvent.purr;
                return true;
            case "purreow":
                result = LevelSoundEvent.purreow;
                return true;
            case "death.min.volume":
                result = LevelSoundEvent.death_min_volume;
                return true;
            case "death.mid.volume":
                result = LevelSoundEvent.death_mid_volume;
                return true;
            case "imitate.blaze":
                result = LevelSoundEvent.imitate_blaze;
                return true;
            case "imitate.cave_spider":
                result = LevelSoundEvent.imitate_cave_spider;
                return true;
            case "imitate.creeper":
                result = LevelSoundEvent.imitate_creeper;
                return true;
            case "imitate.elder_guardian":
                result = LevelSoundEvent.imitate_elder_guardian;
                return true;
            case "imitate.ender_dragon":
                result = LevelSoundEvent.imitate_ender_dragon;
                return true;
            case "imitate.enderman":
                result = LevelSoundEvent.imitate_enderman;
                return true;
            case "imitate.endermite":
                result = LevelSoundEvent.imitate_endermite;
                return true;
            case "imitate.evocation_illager":
                result = LevelSoundEvent.imitate_evocation_illager;
                return true;
            case "imitate.ghast":
                result = LevelSoundEvent.imitate_ghast;
                return true;
            case "imitate.husk":
                result = LevelSoundEvent.imitate_husk;
                return true;
            case "imitate.magma_cube":
                result = LevelSoundEvent.imitate_magma_cube;
                return true;
            case "imitate.polar_bear":
                result = LevelSoundEvent.imitate_polar_bear;
                return true;
            case "imitate.shulker":
                result = LevelSoundEvent.imitate_shulker;
                return true;
            case "imitate.silverfish":
                result = LevelSoundEvent.imitate_silverfish;
                return true;
            case "imitate.skeleton":
                result = LevelSoundEvent.imitate_skeleton;
                return true;
            case "imitate.slime":
                result = LevelSoundEvent.imitate_slime;
                return true;
            case "imitate.spider":
                result = LevelSoundEvent.imitate_spider;
                return true;
            case "imitate.stray":
                result = LevelSoundEvent.imitate_stray;
                return true;
            case "imitate.vex":
                result = LevelSoundEvent.imitate_vex;
                return true;
            case "imitate.vindication_illager":
                result = LevelSoundEvent.imitate_vindication_illager;
                return true;
            case "imitate.witch":
                result = LevelSoundEvent.imitate_witch;
                return true;
            case "imitate.wither":
                result = LevelSoundEvent.imitate_wither;
                return true;
            case "imitate.wither_skeleton":
                result = LevelSoundEvent.imitate_wither_skeleton;
                return true;
            case "imitate.wolf":
                result = LevelSoundEvent.imitate_wolf;
                return true;
            case "imitate.zombie":
                result = LevelSoundEvent.imitate_zombie;
                return true;
            case "imitate.zombie_pigman":
                result = LevelSoundEvent.imitate_zombie_pigman;
                return true;
            case "imitate.zombie_villager":
                result = LevelSoundEvent.imitate_zombie_villager;
                return true;
            case "block.end_portal_frame.fill":
                result = LevelSoundEvent.block_end_portal_frame_fill;
                return true;
            case "block.end_portal.spawn":
                result = LevelSoundEvent.block_end_portal_spawn;
                return true;
            case "random.anvil_use":
                result = LevelSoundEvent.random_anvil_use;
                return true;
            case "bottle.dragonbreath":
                result = LevelSoundEvent.bottle_dragonbreath;
                return true;
            case "balloonpop":
                result = LevelSoundEvent.balloonpop;
                return true;
            case "sparkler.active":
                result = LevelSoundEvent.sparkler_active;
                return true;
            case "item.trident.hit":
                result = LevelSoundEvent.item_trident_hit;
                return true;
            case "item.trident.hit_ground":
                result = LevelSoundEvent.item_trident_hit_ground;
                return true;
            case "item.trident.return":
                result = LevelSoundEvent.item_trident_return;
                return true;
            case "item.trident.riptide_1":
                result = LevelSoundEvent.item_trident_riptide_1;
                return true;
            case "item.trident.riptide_2":
                result = LevelSoundEvent.item_trident_riptide_2;
                return true;
            case "item.trident.riptide_3":
                result = LevelSoundEvent.item_trident_riptide_3;
                return true;
            case "item.trident.throw":
                result = LevelSoundEvent.item_trident_throw;
                return true;
            case "item.trident.thunder":
                result = LevelSoundEvent.item_trident_thunder;
                return true;
            case "block.fletching_table.use":
                result = LevelSoundEvent.block_fletching_table_use;
                return true;
            case "elemconstruct.open":
                result = LevelSoundEvent.elemconstruct_open;
                return true;
            case "icebomb.hit":
                result = LevelSoundEvent.icebomb_hit;
                return true;
            case "lt.reaction.icebomb":
                result = LevelSoundEvent.lt_reaction_icebomb;
                return true;
            case "lt.reaction.bleach":
                result = LevelSoundEvent.lt_reaction_bleach;
                return true;
            case "lt.reaction.epaste":
                result = LevelSoundEvent.lt_reaction_epaste;
                return true;
            case "lt.reaction.epaste2":
                result = LevelSoundEvent.lt_reaction_epaste2;
                return true;
            case "lt.reaction.fertilizer":
                result = LevelSoundEvent.lt_reaction_fertilizer;
                return true;
            case "lt.reaction.fireball":
                result = LevelSoundEvent.lt_reaction_fireball;
                return true;
            case "lt.reaction.mgsalt":
                result = LevelSoundEvent.lt_reaction_mgsalt;
                return true;
            case "lt.reaction.miscfire":
                result = LevelSoundEvent.lt_reaction_miscfire;
                return true;
            case "lt.reaction.fire":
                result = LevelSoundEvent.lt_reaction_fire;
                return true;
            case "lt.reaction.miscexplosion":
                result = LevelSoundEvent.lt_reaction_miscexplosion;
                return true;
            case "lt.reaction.miscmystical":
                result = LevelSoundEvent.lt_reaction_miscmystical;
                return true;
            case "lt.reaction.miscmystical2":
                result = LevelSoundEvent.lt_reaction_miscmystical2;
                return true;
            case "lt.reaction.product":
                result = LevelSoundEvent.lt_reaction_product;
                return true;
            case "sparkler.use":
                result = LevelSoundEvent.sparkler_use;
                return true;
            case "glowstick.use":
                result = LevelSoundEvent.glowstick_use;
                return true;
            case "block.turtle_egg.break":
                result = LevelSoundEvent.block_turtle_egg_break;
                return true;
            case "block.turtle_egg.crack":
                result = LevelSoundEvent.block_turtle_egg_crack;
                return true;
            case "block.turtle_egg.hatch":
                result = LevelSoundEvent.block_turtle_egg_hatch;
                return true;
            case "block.turtle_egg.attack":
                result = LevelSoundEvent.block_turtle_egg_attack;
                return true;
            case "block.sniffer_egg.crack":
                result = LevelSoundEvent.block_sniffer_egg_crack;
                return true;
            case "block.sniffer_egg.hatch":
                result = LevelSoundEvent.block_sniffer_egg_hatch;
                return true;
            case "block.frog_spawn.hatch":
                result = LevelSoundEvent.block_frog_spawn_hatch;
                return true;
            case "block.frog_spawn.break":
                result = LevelSoundEvent.block_frog_spawn_break;
                return true;
            case "swoop":
                result = LevelSoundEvent.swoop;
                return true;
            case "presneeze":
                result = LevelSoundEvent.presneeze;
                return true;
            case "sneeze":
                result = LevelSoundEvent.sneeze;
                return true;
            case "scared":
                result = LevelSoundEvent.scared;
                return true;
            case "ambient.aggressive":
                result = LevelSoundEvent.ambient_aggressive;
                return true;
            case "ambient.worried":
                result = LevelSoundEvent.ambient_worried;
                return true;
            case "cant_breed":
                result = LevelSoundEvent.cant_breed;
                return true;
            case "block.scaffolding.climb":
                result = LevelSoundEvent.block_scaffolding_climb;
                return true;
            case "block.bamboo_sapling.place":
                result = LevelSoundEvent.block_bamboo_sapling_place;
                return true;
            case "crossbow.loading.start":
                result = LevelSoundEvent.crossbow_loading_start;
                return true;
            case "crossbow.loading.middle":
                result = LevelSoundEvent.crossbow_loading_middle;
                return true;
            case "crossbow.loading.end":
                result = LevelSoundEvent.crossbow_loading_end;
                return true;
            case "crossbow.shoot":
                result = LevelSoundEvent.crossbow_shoot;
                return true;
            case "crossbow.quick_charge.start":
                result = LevelSoundEvent.crossbow_quick_charge_start;
                return true;
            case "crossbow.quick_charge.middle":
                result = LevelSoundEvent.crossbow_quick_charge_middle;
                return true;
            case "crossbow.quick_charge.end":
                result = LevelSoundEvent.crossbow_quick_charge_end;
                return true;
            case "item.shield.block":
                result = LevelSoundEvent.item_shield_block;
                return true;
            case "portal.travel":
                result = LevelSoundEvent.portal_travel;
                return true;
            case "item.book.put":
                result = LevelSoundEvent.item_book_put;
                return true;
            case "block.grindstone.use":
                result = LevelSoundEvent.block_grindstone_use;
                return true;
            case "block.bell.hit":
                result = LevelSoundEvent.block_bell_hit;
                return true;
            case "block.campfire.crackle":
                result = LevelSoundEvent.block_campfire_crackle;
                return true;
            case "block.sweet_berry_bush.hurt":
                result = LevelSoundEvent.block_sweet_berry_bush_hurt;
                return true;
            case "block.sweet_berry_bush.pick":
                result = LevelSoundEvent.block_sweet_berry_bush_pick;
                return true;
            case "block.stonecutter.use":
                result = LevelSoundEvent.block_stonecutter_use;
                return true;
            case "block.cartography_table.use":
                result = LevelSoundEvent.block_cartography_table_use;
                return true;
            case "block.composter.empty":
                result = LevelSoundEvent.block_composter_empty;
                return true;
            case "block.composter.fill":
                result = LevelSoundEvent.block_composter_fill;
                return true;
            case "block.composter.fill_success":
                result = LevelSoundEvent.block_composter_fill_success;
                return true;
            case "block.composter.ready":
                result = LevelSoundEvent.block_composter_ready;
                return true;
            case "roar":
                result = LevelSoundEvent.roar;
                return true;
            case "stun":
                result = LevelSoundEvent.stun;
                return true;
            case "block.barrel.open":
                result = LevelSoundEvent.block_barrel_open;
                return true;
            case "block.barrel.close":
                result = LevelSoundEvent.block_barrel_close;
                return true;
            case "raid.horn":
                result = LevelSoundEvent.raid_horn;
                return true;
            case "ui.stonecutter.take_result":
                result = LevelSoundEvent.ui_stonecutter_take_result;
                return true;
            case "ui.cartography_table.take_result":
                result = LevelSoundEvent.ui_cartography_table_take_result;
                return true;
            case "ui.loom.take_result":
                result = LevelSoundEvent.ui_loom_take_result;
                return true;
            case "block.smoker.smoke":
                result = LevelSoundEvent.block_smoker_smoke;
                return true;
            case "block.blastfurnace.fire_crackle":
                result = LevelSoundEvent.block_blastfurnace_fire_crackle;
                return true;
            case "block.smithing_table.use":
                result = LevelSoundEvent.block_smithing_table_use;
                return true;
            case "block.loom.use":
                result = LevelSoundEvent.block_loom_use;
                return true;
            case "ambient.in.raid":
                result = LevelSoundEvent.ambient_in_raid;
                return true;
            case "screech":
                result = LevelSoundEvent.screech;
                return true;
            case "sleep":
                result = LevelSoundEvent.sleep;
                return true;
            case "block.furnace.lit":
                result = LevelSoundEvent.block_furnace_lit;
                return true;
            case "convert_mooshroom":
                result = LevelSoundEvent.convert_mooshroom;
                return true;
            case "milk_suspiciously":
                result = LevelSoundEvent.milk_suspiciously;
                return true;
            case "celebrate":
                result = LevelSoundEvent.celebrate;
                return true;
            case "block.beehive.enter":
                result = LevelSoundEvent.block_beehive_enter;
                return true;
            case "block.beehive.exit":
                result = LevelSoundEvent.block_beehive_exit;
                return true;
            case "block.beehive.shear":
                result = LevelSoundEvent.block_beehive_shear;
                return true;
            case "block.beehive.work":
                result = LevelSoundEvent.block_beehive_work;
                return true;
            case "block.beehive.drip":
                result = LevelSoundEvent.block_beehive_drip;
                return true;
            case "ambient.cave":
                result = LevelSoundEvent.ambient_cave;
                return true;
            case "angry":
                result = LevelSoundEvent.angry;
                return true;
            case "retreat":
                result = LevelSoundEvent.retreat;
                return true;
            case "converted_to_zombified":
                result = LevelSoundEvent.converted_to_zombified;
                return true;
            case "step_lava":
                result = LevelSoundEvent.step_lava;
                return true;
            case "tempt":
                result = LevelSoundEvent.tempt;
                return true;
            case "panic":
                result = LevelSoundEvent.panic;
                return true;
            case "admire":
                result = LevelSoundEvent.admire;
                return true;
            case "particle.soul_escape.quiet":
                result = LevelSoundEvent.particle_soul_escape_quiet;
                return true;
            case "particle.soul_escape.loud":
                result = LevelSoundEvent.particle_soul_escape_loud;
                return true;
            case "respawn_anchor.charge":
                result = LevelSoundEvent.respawn_anchor_charge;
                return true;
            case "respawn_anchor.deplete":
                result = LevelSoundEvent.respawn_anchor_deplete;
                return true;
            case "respawn_anchor.set_spawn":
                result = LevelSoundEvent.respawn_anchor_set_spawn;
                return true;
            case "respawn_anchor.ambient":
                result = LevelSoundEvent.respawn_anchor_ambient;
                return true;
            case "ambient.crimson_forest.mood":
                result = LevelSoundEvent.ambient_crimson_forest_mood;
                return true;
            case "ambient.warped_forest.mood":
                result = LevelSoundEvent.ambient_warped_forest_mood;
                return true;
            case "ambient.soulsand_valley.mood":
                result = LevelSoundEvent.ambient_soulsand_valley_mood;
                return true;
            case "ambient.nether_wastes.mood":
                result = LevelSoundEvent.ambient_nether_wastes_mood;
                return true;
            case "ambient.crimson_forest.additions":
                result = LevelSoundEvent.ambient_crimson_forest_additions;
                return true;
            case "ambient.warped_forest.additions":
                result = LevelSoundEvent.ambient_warped_forest_additions;
                return true;
            case "ambient.soulsand_valley.additions":
                result = LevelSoundEvent.ambient_soulsand_valley_additions;
                return true;
            case "ambient.nether_wastes.additions":
                result = LevelSoundEvent.ambient_nether_wastes_additions;
                return true;
            case "ambient.basalt_deltas.additions":
                result = LevelSoundEvent.ambient_basalt_deltas_additions;
                return true;
            case "ambient.crimson_forest.loop":
                result = LevelSoundEvent.ambient_crimson_forest_loop;
                return true;
            case "ambient.warped_forest.loop":
                result = LevelSoundEvent.ambient_warped_forest_loop;
                return true;
            case "ambient.soulsand_valley.loop":
                result = LevelSoundEvent.ambient_soulsand_valley_loop;
                return true;
            case "ambient.nether_wastes.loop":
                result = LevelSoundEvent.ambient_nether_wastes_loop;
                return true;
            case "ambient.basalt_deltas.loop":
                result = LevelSoundEvent.ambient_basalt_deltas_loop;
                return true;
            case "lodestone_compass.link_compass_to_lodestone":
                result = LevelSoundEvent.lodestone_compass_link_compass_to_lodestone;
                return true;
            case "ambient.basalt_deltas.mood":
                result = LevelSoundEvent.ambient_basalt_deltas_mood;
                return true;
            case "power.on.sculk_sensor":
                result = LevelSoundEvent.power_on_sculk_sensor;
                return true;
            case "power.off.sculk_sensor":
                result = LevelSoundEvent.power_off_sculk_sensor;
                return true;
            case "smithing_table.use":
                result = LevelSoundEvent.smithing_table_use;
                return true;
            case "default":
                result = LevelSoundEvent.Default;
                return true;
            case "lay_egg":
                result = LevelSoundEvent.lay_egg;
                return true;
            case "lay_spawn":
                result = LevelSoundEvent.lay_spawn;
                return true;
            case "bucket.fill.powder_snow":
                result = LevelSoundEvent.bucket_fill_powder_snow;
                return true;
            case "bucket.empty.powder_snow":
                result = LevelSoundEvent.bucket_empty_powder_snow;
                return true;
            case "cauldron_drip.water.pointed_dripstone":
                result = LevelSoundEvent.cauldron_drip_water_pointed_dripstone;
                return true;
            case "cauldron_drip.lava.pointed_dripstone":
                result = LevelSoundEvent.cauldron_drip_lava_pointed_dripstone;
                return true;
            case "tilt_down.big_dripleaf":
                result = LevelSoundEvent.tilt_down_big_dripleaf;
                return true;
            case "tilt_up.big_dripleaf":
                result = LevelSoundEvent.tilt_up_big_dripleaf;
                return true;
            case "drip.water.pointed_dripstone":
                result = LevelSoundEvent.drip_water_pointed_dripstone;
                return true;
            case "pick_berries.cave_vines":
                result = LevelSoundEvent.pick_berries_cave_vines;
                return true;
            case "drip.lava.pointed_dripstone":
                result = LevelSoundEvent.drip_lava_pointed_dripstone;
                return true;
            case "copper.wax.on":
                result = LevelSoundEvent.copper_wax_on;
                return true;
            case "copper.wax.off":
                result = LevelSoundEvent.copper_wax_off;
                return true;
            case "scrape":
                result = LevelSoundEvent.scrape;
                return true;
            case "item.spyglass.use":
                result = LevelSoundEvent.item_spyglass_use;
                return true;
            case "item.spyglass.stop_using":
                result = LevelSoundEvent.item_spyglass_stop_using;
                return true;
            case "chime.amethyst_block":
                result = LevelSoundEvent.chime_amethyst_block;
                return true;
            case "mob.player.hurt_drown":
                result = LevelSoundEvent.mob_player_hurt_drown;
                return true;
            case "mob.player.hurt_on_fire":
                result = LevelSoundEvent.mob_player_hurt_on_fire;
                return true;
            case "mob.player.hurt_freeze":
                result = LevelSoundEvent.mob_player_hurt_freeze;
                return true;
            case "ambient.screamer":
                result = LevelSoundEvent.ambient_screamer;
                return true;
            case "hurt.screamer":
                result = LevelSoundEvent.hurt_screamer;
                return true;
            case "death.screamer":
                result = LevelSoundEvent.death_screamer;
                return true;
            case "milk.screamer":
                result = LevelSoundEvent.milk_screamer;
                return true;
            case "jump_to_block":
                result = LevelSoundEvent.jump_to_block;
                return true;
            case "pre_ram":
                result = LevelSoundEvent.pre_ram;
                return true;
            case "pre_ram.screamer":
                result = LevelSoundEvent.pre_ram_screamer;
                return true;
            case "ram_impact":
                result = LevelSoundEvent.ram_impact;
                return true;
            case "ram_impact.screamer":
                result = LevelSoundEvent.ram_impact_screamer;
                return true;
            case "squid.ink_squirt":
                result = LevelSoundEvent.squid_ink_squirt;
                return true;
            case "glow_squid.ink_squirt":
                result = LevelSoundEvent.glow_squid_ink_squirt;
                return true;
            case "convert_to_stray":
                result = LevelSoundEvent.convert_to_stray;
                return true;
            case "cake.add_candle":
                result = LevelSoundEvent.cake_add_candle;
                return true;
            case "extinguish.candle":
                result = LevelSoundEvent.extinguish_candle;
                return true;
            case "ambient.candle":
                result = LevelSoundEvent.ambient_candle;
                return true;
            case "block.click":
                result = LevelSoundEvent.block_click;
                return true;
            case "block.click.fail":
                result = LevelSoundEvent.block_click_fail;
                return true;
            case "block.sculk_catalyst.bloom":
                result = LevelSoundEvent.block_sculk_catalyst_bloom;
                return true;
            case "block.sculk_shrieker.shriek":
                result = LevelSoundEvent.block_sculk_shrieker_shriek;
                return true;
            case "nearby_close":
                result = LevelSoundEvent.nearby_close;
                return true;
            case "nearby_closer":
                result = LevelSoundEvent.nearby_closer;
                return true;
            case "nearby_closest":
                result = LevelSoundEvent.nearby_closest;
                return true;
            case "agitated":
                result = LevelSoundEvent.agitated;
                return true;
            case "listening":
                result = LevelSoundEvent.listening;
                return true;
            case "heartbeat":
                result = LevelSoundEvent.heartbeat;
                return true;
            case "tongue":
                result = LevelSoundEvent.tongue;
                return true;
            case "item_given":
                result = LevelSoundEvent.item_given;
                return true;
            case "item_taken":
                result = LevelSoundEvent.item_taken;
                return true;
            case "item_thrown":
                result = LevelSoundEvent.item_thrown;
                return true;
            case "irongolem.crack":
                result = LevelSoundEvent.irongolem_crack;
                return true;
            case "irongolem.repair":
                result = LevelSoundEvent.irongolem_repair;
                return true;
            case "horn_break":
                result = LevelSoundEvent.horn_break;
                return true;
            case "horn_call0":
                result = LevelSoundEvent.horn_call0;
                return true;
            case "horn_call1":
                result = LevelSoundEvent.horn_call1;
                return true;
            case "horn_call2":
                result = LevelSoundEvent.horn_call2;
                return true;
            case "horn_call3":
                result = LevelSoundEvent.horn_call3;
                return true;
            case "horn_call4":
                result = LevelSoundEvent.horn_call4;
                return true;
            case "horn_call5":
                result = LevelSoundEvent.horn_call5;
                return true;
            case "horn_call6":
                result = LevelSoundEvent.horn_call6;
                return true;
            case "horn_call7":
                result = LevelSoundEvent.horn_call7;
                return true;
            case "imitate.warden":
                result = LevelSoundEvent.imitate_warden;
                return true;
            case "listening_angry":
                result = LevelSoundEvent.listening_angry;
                return true;
            case "sonic_boom":
                result = LevelSoundEvent.sonic_boom;
                return true;
            case "sonic_charge":
                result = LevelSoundEvent.sonic_charge;
                return true;
            case "convert_to_frog":
                result = LevelSoundEvent.convert_to_frog;
                return true;
            case "block.sculk.spread":
                result = LevelSoundEvent.block_sculk_spread;
                return true;
            case "charge.sculk":
                result = LevelSoundEvent.charge_sculk;
                return true;
            case "block.sculk_sensor.place":
                result = LevelSoundEvent.block_sculk_sensor_place;
                return true;
            case "block.sculk_shrieker.place":
                result = LevelSoundEvent.block_sculk_shrieker_place;
                return true;
            case "block.enchanting_table.use":
                result = LevelSoundEvent.block_enchanting_table_use;
                return true;
            case "bundle.drop_contents":
                result = LevelSoundEvent.bundle_drop_contents;
                return true;
            case "bundle.insert":
                result = LevelSoundEvent.bundle_insert;
                return true;
            case "bundle.insert_fail":
                result = LevelSoundEvent.bundle_insert_fail;
                return true;
            case "bundle.remove_one":
                result = LevelSoundEvent.bundle_remove_one;
                return true;
            case "step_sand":
                result = LevelSoundEvent.step_sand;
                return true;
            case "dash_ready":
                result = LevelSoundEvent.dash_ready;
                return true;
            case "pressure_plate.click_off":
                result = LevelSoundEvent.pressure_plate_click_off;
                return true;
            case "pressure_plate.click_on":
                result = LevelSoundEvent.pressure_plate_click_on;
                return true;
            case "button.click_off":
                result = LevelSoundEvent.button_click_off;
                return true;
            case "button.click_on":
                result = LevelSoundEvent.button_click_on;
                return true;
            case "door.open":
                result = LevelSoundEvent.door_open;
                return true;
            case "door.close":
                result = LevelSoundEvent.door_close;
                return true;
            case "trapdoor.open":
                result = LevelSoundEvent.trapdoor_open;
                return true;
            case "trapdoor.close":
                result = LevelSoundEvent.trapdoor_close;
                return true;
            case "fence_gate.open":
                result = LevelSoundEvent.fence_gate_open;
                return true;
            case "fence_gate.close":
                result = LevelSoundEvent.fence_gate_close;
                return true;
            case "insert":
                result = LevelSoundEvent.insert;
                return true;
            case "pickup":
                result = LevelSoundEvent.pickup;
                return true;
            case "insert_enchanted":
                result = LevelSoundEvent.insert_enchanted;
                return true;
            case "pickup_enchanted":
                result = LevelSoundEvent.pickup_enchanted;
                return true;
            case "shatter_pot":
                result = LevelSoundEvent.shatter_pot;
                return true;
            case "break_pot":
                result = LevelSoundEvent.break_pot;
                return true;
            case "brush":
                result = LevelSoundEvent.brush;
                return true;
            case "brush_completed":
                result = LevelSoundEvent.brush_completed;
                return true;
            case "block.sign.waxed_interact_fail":
                result = LevelSoundEvent.block_sign_waxed_interact_fail;
                return true;
            case "note.bass":
                result = LevelSoundEvent.note_bass;
                return true;
            case "pumpkin.carve":
                result = LevelSoundEvent.pumpkin_carve;
                return true;
            case "mob.husk.convert_to_zombie":
                result = LevelSoundEvent.mob_husk_convert_to_zombie;
                return true;
            case "mob.pig.death":
                result = LevelSoundEvent.mob_pig_death;
                return true;
            case "mob.hoglin.converted_to_zombified":
                result = LevelSoundEvent.mob_hoglin_converted_to_zombified;
                return true;
            case "ambient.underwater.enter":
                result = LevelSoundEvent.ambient_underwater_enter;
                return true;
            case "ambient.underwater.exit":
                result = LevelSoundEvent.ambient_underwater_exit;
                return true;
            case "bottle.fill":
                result = LevelSoundEvent.bottle_fill;
                return true;
            case "bottle.empty":
                result = LevelSoundEvent.bottle_empty;
                return true;
            case "block.decorated_pot.insert":
                result = LevelSoundEvent.block_decorated_pot_insert;
                return true;
            case "block.decorated_pot.insert_fail":
                result = LevelSoundEvent.block_decorated_pot_insert_fail;
                return true;
            case "crafter.craft":
                result = LevelSoundEvent.crafter_craft;
                return true;
            case "crafter.fail":
                result = LevelSoundEvent.crafter_fail;
                return true;
            case "crafter.disable_slot":
                result = LevelSoundEvent.crafter_disable_slot;
                return true;
            case "block.copper_bulb.turn_on":
                result = LevelSoundEvent.block_copper_bulb_turn_on;
                return true;
            case "block.copper_bulb.turn_off":
                result = LevelSoundEvent.block_copper_bulb_turn_off;
                return true;
            case "breeze_wind_charge.burst":
                result = LevelSoundEvent.breeze_wind_charge_burst;
                return true;
            case "imitate.breeze":
                result = LevelSoundEvent.imitate_breeze;
                return true;
            case "trial_spawner.open_shutter":
                result = LevelSoundEvent.trial_spawner_open_shutter;
                return true;
            case "trial_spawner.detect_player":
                result = LevelSoundEvent.trial_spawner_detect_player;
                return true;
            case "trial_spawner.close_shutter":
                result = LevelSoundEvent.trial_spawner_close_shutter;
                return true;
            case "trial_spawner.spawn_mob":
                result = LevelSoundEvent.trial_spawner_spawn_mob;
                return true;
            case "trial_spawner.eject_item":
                result = LevelSoundEvent.trial_spawner_eject_item;
                return true;
            case "trial_spawner.ambient":
                result = LevelSoundEvent.trial_spawner_ambient;
                return true;
            case "mob.armadillo.brush":
                result = LevelSoundEvent.mob_armadillo_brush;
                return true;
            case "mob.armadillo.scute_drop":
                result = LevelSoundEvent.mob_armadillo_scute_drop;
                return true;
            case "armor.equip_wolf":
                result = LevelSoundEvent.armor_equip_wolf;
                return true;
            case "armor.unequip_wolf":
                result = LevelSoundEvent.armor_unequip_wolf;
                return true;
            case "reflect":
                result = LevelSoundEvent.reflect;
                return true;
            case "vault.open_shutter":
                result = LevelSoundEvent.vault_open_shutter;
                return true;
            case "vault.close_shutter":
                result = LevelSoundEvent.vault_close_shutter;
                return true;
            case "vault.eject_item":
                result = LevelSoundEvent.vault_eject_item;
                return true;
            case "vault.insert_item":
                result = LevelSoundEvent.vault_insert_item;
                return true;
            case "vault.insert_item_fail":
                result = LevelSoundEvent.vault_insert_item_fail;
                return true;
            case "vault.ambient":
                result = LevelSoundEvent.vault_ambient;
                return true;
            case "vault.activate":
                result = LevelSoundEvent.vault_activate;
                return true;
            case "vault.deactivate":
                result = LevelSoundEvent.vault_deactivate;
                return true;
            case "hurt.reduced":
                result = LevelSoundEvent.hurt_reduced;
                return true;
            case "wind_charge.burst":
                result = LevelSoundEvent.wind_charge_burst;
                return true;
            case "armor.break_wolf":
                result = LevelSoundEvent.armor_break_wolf;
                return true;
            case "armor.crack_wolf":
                result = LevelSoundEvent.armor_crack_wolf;
                return true;
            case "armor.repair_wolf":
                result = LevelSoundEvent.armor_repair_wolf;
                return true;
            case "mace.smash_air":
                result = LevelSoundEvent.mace_smash_air;
                return true;
            case "mace.smash_ground":
                result = LevelSoundEvent.mace_smash_ground;
                return true;
            case "mace.heavy_smash_ground":
                result = LevelSoundEvent.mace_heavy_smash_ground;
                return true;
            case "trial_spawner.charge_activate":
                result = LevelSoundEvent.trial_spawner_charge_activate;
                return true;
            case "trial_spawner.ambient_ominous":
                result = LevelSoundEvent.trial_spawner_ambient_ominous;
                return true;
            case "apply_effect.bad_omen":
                result = LevelSoundEvent.apply_effect_bad_omen;
                return true;
            case "apply_effect.raid_omen":
                result = LevelSoundEvent.apply_effect_raid_omen;
                return true;
            case "apply_effect.trial_omen":
                result = LevelSoundEvent.apply_effect_trial_omen;
                return true;
            case "ominous_item_spawner.spawn_item":
                result = LevelSoundEvent.ominous_item_spawner_spawn_item;
                return true;
            case "ominous_bottle.end_use":
                result = LevelSoundEvent.ominous_bottle_end_use;
                return true;
            case "ominous_item_spawner.spawn_item_begin":
                result = LevelSoundEvent.ominous_item_spawner_spawn_item_begin;
                return true;
            case "ominous_item_spawner.about_to_spawn_item":
                result = LevelSoundEvent.ominous_item_spawner_about_to_spawn_item;
                return true;
            case "imitate.bogged":
                result = LevelSoundEvent.imitate_bogged;
                return true;
            case "vault.reject_rewarded_player":
                result = LevelSoundEvent.vault_reject_rewarded_player;
                return true;
            case "imitate.drowned":
                result = LevelSoundEvent.imitate_drowned;
                return true;
            case "sponge.absorb":
                result = LevelSoundEvent.sponge_absorb;
                return true;
            case "imitate.creaking":
                result = LevelSoundEvent.imitate_creaking;
                return true;
            case "block.creaking_heart.trail":
                result = LevelSoundEvent.block_creaking_heart_trail;
                return true;
            case "creaking_heart_spawn":
                result = LevelSoundEvent.creaking_heart_spawn;
                return true;
            case "activate":
                result = LevelSoundEvent.activate;
                return true;
            case "deactivate":
                result = LevelSoundEvent.deactivate;
                return true;
            case "freeze":
                result = LevelSoundEvent.freeze;
                return true;
            case "unfreeze":
                result = LevelSoundEvent.unfreeze;
                return true;
            case "open":
                result = LevelSoundEvent.open;
                return true;
            case "open_long":
                result = LevelSoundEvent.open_long;
                return true;
            case "close":
                result = LevelSoundEvent.close;
                return true;
            case "close_long":
                result = LevelSoundEvent.close_long;
                return true;
            case "imitate.phantom":
                result = LevelSoundEvent.imitate_phantom;
                return true;
            case "imitate.zoglin":
                result = LevelSoundEvent.imitate_zoglin;
                return true;
            case "imitate.guardian":
                result = LevelSoundEvent.imitate_guardian;
                return true;
            case "imitate.ravager":
                result = LevelSoundEvent.imitate_ravager;
                return true;
            case "imitate.pillager":
                result = LevelSoundEvent.imitate_pillager;
                return true;
            case "place_in_water":
                result = LevelSoundEvent.place_in_water;
                return true;
            case "state_change":
                result = LevelSoundEvent.state_change;
                return true;
            case "imitate.happy_ghast":
                result = LevelSoundEvent.imitate_happy_ghast;
                return true;
            case "armor.unequip_generic":
                result = LevelSoundEvent.armor_unequip_generic;
                return true;
            case "ambient.weather.the_end_light_flash":
                result = LevelSoundEvent.ambient_weather_the_end_light_flash;
                return true;
            case "lead.leash":
                result = LevelSoundEvent.lead_leash;
                return true;
            case "lead.unleash":
                result = LevelSoundEvent.lead_unleash;
                return true;
            case "lead.break":
                result = LevelSoundEvent.lead_break;
                return true;
            case "unsaddle":
                result = LevelSoundEvent.unsaddle;
                return true;
            case "armor.equip_copper":
                result = LevelSoundEvent.armor_equip_copper;
                return true;
            case "place_item":
                result = LevelSoundEvent.place_item;
                return true;
            case "single_swap":
                result = LevelSoundEvent.single_swap;
                return true;
            case "multi_swap":
                result = LevelSoundEvent.multi_swap;
                return true;
            case "item.enchant.lunge1":
                result = LevelSoundEvent.item_enchant_lunge1;
                return true;
            case "item.enchant.lunge2":
                result = LevelSoundEvent.item_enchant_lunge2;
                return true;
            case "item.enchant.lunge3":
                result = LevelSoundEvent.item_enchant_lunge3;
                return true;
            case "attack.critical":
                result = LevelSoundEvent.attack_critical;
                return true;
            case "item.spear.attack_hit":
                result = LevelSoundEvent.item_spear_attack_hit;
                return true;
            case "item.spear.attack_miss":
                result = LevelSoundEvent.item_spear_attack_miss;
                return true;
            case "item.wooden_spear.attack_hit":
                result = LevelSoundEvent.item_wooden_spear_attack_hit;
                return true;
            case "item.wooden_spear.attack_miss":
                result = LevelSoundEvent.item_wooden_spear_attack_miss;
                return true;
            case "imitate.parched":
                result = LevelSoundEvent.imitate_parched;
                return true;
            case "imitate.camel_husk":
                result = LevelSoundEvent.imitate_camel_husk;
                return true;
            case "item.spear.use":
                result = LevelSoundEvent.item_spear_use;
                return true;
            case "item.wooden_spear.use":
                result = LevelSoundEvent.item_wooden_spear_use;
                return true;
            case "saddle_in_water":
                result = LevelSoundEvent.saddle_in_water;
                return true;
            case "item.stone_spear.attack_hit":
                result = LevelSoundEvent.item_stone_spear_attack_hit;
                return true;
            case "item.iron_spear.attack_hit":
                result = LevelSoundEvent.item_iron_spear_attack_hit;
                return true;
            case "item.copper_spear.attack_hit":
                result = LevelSoundEvent.item_copper_spear_attack_hit;
                return true;
            case "item.golden_spear.attack_hit":
                result = LevelSoundEvent.item_golden_spear_attack_hit;
                return true;
            case "item.diamond_spear.attack_hit":
                result = LevelSoundEvent.item_diamond_spear_attack_hit;
                return true;
            case "item.netherite_spear.attack_hit":
                result = LevelSoundEvent.item_netherite_spear_attack_hit;
                return true;
            case "item.stone_spear.attack_miss":
                result = LevelSoundEvent.item_stone_spear_attack_miss;
                return true;
            case "item.iron_spear.attack_miss":
                result = LevelSoundEvent.item_iron_spear_attack_miss;
                return true;
            case "item.copper_spear.attack_miss":
                result = LevelSoundEvent.item_copper_spear_attack_miss;
                return true;
            case "item.golden_spear.attack_miss":
                result = LevelSoundEvent.item_golden_spear_attack_miss;
                return true;
            case "item.diamond_spear.attack_miss":
                result = LevelSoundEvent.item_diamond_spear_attack_miss;
                return true;
            case "item.netherite_spear.attack_miss":
                result = LevelSoundEvent.item_netherite_spear_attack_miss;
                return true;
            case "item.stone_spear.use":
                result = LevelSoundEvent.item_stone_spear_use;
                return true;
            case "item.iron_spear.use":
                result = LevelSoundEvent.item_iron_spear_use;
                return true;
            case "item.copper_spear.use":
                result = LevelSoundEvent.item_copper_spear_use;
                return true;
            case "item.golden_spear.use":
                result = LevelSoundEvent.item_golden_spear_use;
                return true;
            case "item.diamond_spear.use":
                result = LevelSoundEvent.item_diamond_spear_use;
                return true;
            case "item.netherite_spear.use":
                result = LevelSoundEvent.item_netherite_spear_use;
                return true;
            case "pause_growth":
                result = LevelSoundEvent.pause_growth;
                return true;
            case "reset_growth":
                result = LevelSoundEvent.reset_growth;
                return true;
            case "pushed_by_player":
                result = LevelSoundEvent.pushed_by_player;
                return true;
            case "bounce":
                result = LevelSoundEvent.bounce;
                return true;
            case "slime_landing":
                result = LevelSoundEvent.slime_landing;
                return true;
            case "absorb_block":
                result = LevelSoundEvent.absorb_block;
                return true;
            case "eject_block":
                result = LevelSoundEvent.eject_block;
                return true;
            case "geyser_eruption_start":
                result = LevelSoundEvent.geyser_eruption_start;
                return true;
            case "geyser_eruption_active":
                result = LevelSoundEvent.geyser_eruption_active;
                return true;
            case "record.bounce":
                result = LevelSoundEvent.record_bounce;
                return true;
            case "bucket.fill.land_animal":
                result = LevelSoundEvent.bucket_fill_land_animal;
                return true;
            case "bucket.empty.land_animal":
                result = LevelSoundEvent.bucket_empty_land_animal;
                return true;
            case "geyser_continuous_eruption_start":
                result = LevelSoundEvent.geyser_continuous_eruption_start;
                return true;
            case "geyser_continuous_eruption_active":
                result = LevelSoundEvent.geyser_continuous_eruption_active;
                return true;
            case "mount":
                result = LevelSoundEvent.mount;
                return true;
            case "dismount":
                result = LevelSoundEvent.dismount;
                return true;
            case "straw_bed.break_leave":
                result = LevelSoundEvent.straw_bed_break_leave;
                return true;
            case "undefined":
                result = LevelSoundEvent.undefined;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
