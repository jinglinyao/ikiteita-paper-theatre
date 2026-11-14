#!/bin/bash
# close-up still sequences for the independent reviews of the properties (dir seq_props) and of the paper people (dir seq_people)
#   name  t0  t1  n   camera x y z   look-at x y z   focal  f-stop
cd /e/sucai/ikiteita || exit 1
rm -rf unity/Capture/seq_props unity/Capture/seq_people
run() { bash tools/sets.sh FilmSeq "[$3,$4,$5,$6,$7,$8,$9,${10},${11},${12},${13},\"$1\",\"$2\"]" 600 2>&1 | grep -E "stills|rror" | cut -c1-160; }
P=seq_props
run $P a_snow_curtain   15.2  18.7 12   0 1.5 -2.6    0 1.6 1.0     28 5.6
run $P b_tape_street    33.6  35.8  8   0 1.3 -1.6    0 1.0 2.0     28 5.6
run $P c_rain           46.6  54.2 12   0 1.3 -1.2    0 1.6 2.0     24 5.6
run $P d_pool_on_paper  63.9  72.5  8   0.62 2.4 0.3  0.62 0 1.52   35 5.6
run $P e_hand_paper     72.5  84.4 26   1.5 0.35 0.5  0.62 0.02 1.52 50 5.6
run $P f_tapes_cross    83.3  85.5  8   0 1.5 -2.6    0 1.5 0.1     24 5.6
run $P g_tapes_off      94.1  96.3  6   0 1.5 -2.6    0 1.5 0.1     24 5.6
run $P h_paper_day      95.2 105.0 20   0 1.5 -2.6    0 1.8 3.0     24 5.6
run $P i_strike        122.2 124.6 12   0 1.5 -2.6    0 1.8 3.0     24 5.6
run $P j_folds         128.7 133.1 22   1.15 0.30 0.95 0.62 0.10 1.52 70 5.6
run $P k_return        133.1 137.9 14   0 1.5 -2.6    0 1.8 3.0     24 5.6
run $P l_bird_flight   133.1 149.5 34   0 1.6 -2.6    0.3 1.7 2.0   24 5.6
run $P m_bird_hand     148.2 171.0 26   -0.2 1.4 -0.6 -0.4 1.3 1.3  35 5.6
run $P n_end           178.3 185.7 16   0 1.5 -2.6    0 1.8 3.0     24 5.6
run $P o_lamp_click    24.6  25.8  8   -0.35 1.55 0.27 0.35 2.55 2.32  50 2.8
run $P p_last_bulb    182.0 185.6 10   0.35 0.30 -1.55 1.33 0.25 -0.04 50 4.0
Q=seq_people
run $Q a_entries        29.3  37.9 26   0 1.3 -1.6    0 1.0 2.22    24 5.6
run $Q b_slips          37.9  42.3 26   0.1 1.25 -1.3 0.1 1.0 2.22  28 5.6
run $Q c_crow           42.3  44.4  8   -0.2 1.45 0.9 -0.35 1.42 2.22 70 5.6
run $Q d_exit1          53.0  55.3  8   0 1.3 -1.6    0 1.0 2.22    24 5.6
run $Q e_back_in        61.7  64.2 10   0 1.3 -1.6    0 1.0 2.22    24 5.6
run $Q f_from_behind    64.5  66.5  3   0.3 1.3 3.9   0.62 0.4 1.52 28 5.6
run $Q g_lean           74.7  80.0 10   -1.1 1.3 0.6  -1.1 1.3 2.22 70 5.6
run $Q h_speech         85.5  96.3 26   0.1 1.35 -1.5 0.1 1.2 2.22  28 5.6
run $Q i_exit2         100.6 105.0  8   0 1.3 -1.6    0 1.0 2.22    24 5.6
run $Q j_return        133.1 137.9 12   0 1.3 -1.6    0 1.0 2.22    24 5.6
run $Q k_lift_lamps    152.5 163.5 30   0.1 1.25 -1.3 0.1 1.0 2.22  28 5.6
run $Q l_lamps_out     178.3 181.5 10   0.1 1.25 -1.3 0.1 1.0 2.22  28 5.6
run $Q m_side_reading   41.0  41.0  1   -2.6 1.4 1.3  -1.35 1.35 2.22 70 5.6
run $Q n_side_lifted   156.5 156.5  1   -2.6 1.4 1.3  -1.35 1.35 2.22 70 5.6
run $Q o_wolf_lamp     172.0 174.0  3   0.3 1.45 0.9  0.5 1.35 2.22 85 4.0
echo done
