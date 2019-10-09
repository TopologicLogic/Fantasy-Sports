Imports Encog.MathUtil
Imports Encog.ML.Genetic
Imports Encog.ML.Genetic.Genes
Imports Encog.ML.Genetic.Genome

Public Class Form1

    <Serializable()> _
    Private Structure player_stats
        Dim name As String
        Dim position As String
        Dim team_ID As String
        Dim team_name As String
        Dim height As Double
        Dim weight As Double
        Dim minutes As Double
        Dim fgm As Double
        Dim fga As Double
        Dim fgp As Double
        Dim tpm As Double
        Dim tpa As Double
        Dim tpp As Double
        Dim ftm As Double
        Dim fta As Double
        Dim ftp As Double
        Dim reb As Double
        Dim ast As Double
        Dim blk As Double
        Dim stl As Double
        Dim pf As Double
        Dim too As Double
        Dim pts As Double
        Dim fp As Double
        Dim fpp() As Double
        Dim mp() As Double
        Dim count As Integer
    End Structure

    Private Structure player_info
        Dim position As String
        Dim name As String
        Dim salary As Double
        Dim game_info As String
        Dim avg As Double
        Dim team As String
    End Structure


    Private Shared rand As New Random()

    Private Shared pg As New ArrayList
    Private Shared sg As New ArrayList
    Private Shared sf As New ArrayList
    Private Shared pf As New ArrayList
    Private Shared c As New ArrayList
    Private Shared g As New ArrayList
    Private Shared f As New ArrayList
    Private Shared u As New ArrayList

    Private Const POPULATION_SIZE As Integer = 10000
    Private Const MUTATION_PERCENT As Double = 0.2
    Private Const PERCENT_TO_MATE As Double = 0.24
    Private Const MATING_POPULATION_PERCENT As Double = 0.5
    Private Const MAX_SAME_SOLUTION As Integer = 50 '250

    Private Shared best_organism() As Integer = Nothing

    Private Shared teams As New SortedList
    Private Shared averages As New SortedList
    Private Shared scores_by_player As New SortedList

    Private Shared model As Encog.Neural.Networks.BasicNetwork = Nothing
    Private Shared model2 As Encog.ML.SVM.SupportVectorMachine = Nothing


    Private Sub loadBinaryData(ByVal filename As String, ByRef data As Object)
        Dim bf As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
        Dim fs As New System.IO.FileStream(filename, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
        data = bf.Deserialize(fs)
        fs.Close()
        fs.Dispose()
    End Sub

    Private Sub saveBinaryData(ByVal filename As String, ByRef data As Object)
        Dim bf As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
        Dim fs As New System.IO.FileStream(filename, IO.FileMode.Create)
        bf.Serialize(fs, data)
        fs.Close()
        fs.Dispose()
    End Sub

    Private Sub RandomizeArray(ByRef items As Array)
        Dim max_index As Integer = items.Length - 1
        For i As Integer = 0 To max_index - 1
            ' Pick an item for position i.
            Dim j As Integer = rand.Next(i, max_index + 1)
            ' Swap them.
            Dim temp As Object = items(i)
            items(i) = items(j)
            items(j) = temp
        Next i
    End Sub

    Private Sub loadDataToListView(ByRef data() As String, ByRef lv As ListView)
        lv.Items.Clear()
        For i As Integer = 1 To data.Length - 1
            Dim ds() As String = data(i).Split(",")
            For j As Integer = 0 To ds.Length - 1
                ds(j) = ds(j).Replace("""", "")
            Next
            Dim lvi As New ListViewItem()
            lvi.Text = ds(1)
            lvi.SubItems.Add(ds(0))
            For j As Integer = 2 To ds.Length - 2
                lvi.SubItems.Add(ds(j))
            Next
            lvi.SubItems.Add(ds(ds.Length - 1).ToUpper)
            lvi.Checked = True
            lv.Items.Add(lvi)
        Next
    End Sub

    Private Sub getAverages2(ByVal raw_data As String)
        ' Name          Position    Team ID     Team Name               #   Height  WEIGHT  SEASON  DATE        LOCATION    OPP
        ' Josh Smith	SF	        LAC	        Los Angeles Clippers	5	81	    225	    POST	Wed 5/27	AWAY	    GS	   
        '
        ' Minutes   FGM FGA FG%     3PM 3PA 3P%     FTM FTA FT%     REB AST BLK STL PF  TO  PTS FP
        ' 21	    3	14	0.214	2	7	0.286	3	6	0.5	    4	1	2	0	2	0	11	22.5																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																			

        Dim sr As New System.IO.StreamReader(raw_data)

        While (Not sr.EndOfStream)

            Dim data() As String = sr.ReadLine.Split(",")

            Dim ps As New player_stats
            ps.fpp = New Double(90) {}
            ps.mp = New Double(50) {}

            Dim ki As Integer = averages.IndexOfKey(data(0))

            If ki >= 0 Then ps = averages.GetByIndex(ki)

            With ps
                .name = data(0)
                .position = data(1)
                .team_ID = data(2)
                .team_name = data(3)
                .height = CInt(data(5))
                .weight = CInt(data(6))
                .minutes += CDbl(data(11))
                .fgm += CDbl(data(12))
                .fga += CDbl(data(13))
                .fgp += CDbl(data(14))
                .tpm += CDbl(data(15))
                .tpa += CDbl(data(16))
                .tpp += CDbl(data(17))
                .ftm += CDbl(data(18))
                .fta += CDbl(data(19))
                .ftp += CDbl(data(20))
                .reb += CDbl(data(21))
                .ast += CDbl(data(22))
                .blk += CDbl(data(23))
                .stl += CDbl(data(24))
                .pf += CDbl(data(25))
                .too += CDbl(data(26))
                .pts += CDbl(data(27))
                .fp += CDbl(data(28))
                .fpp(Math.Max(0, Math.Min(.fpp.Length - 1, Math.Round(CDbl(data(28)))))) += 1
                .mp(Math.Max(0, Math.Min(.mp.Length - 1, Math.Round(CDbl(data(11)))))) += 1
                .count += 1
            End With

            'With max_stats
            '    .height = Math.Max(.height, CInt(data(5)))
            '    .weight = Math.Max(.weight, CInt(data(6)))
            '    .minutes = Math.Max(.minutes, CDbl(data(11)))
            '    .fgm = Math.Max(.fgm, CDbl(data(12)))
            '    .fga = Math.Max(.fga, CDbl(data(13)))
            '    .fgp = Math.Max(.fgp, CDbl(data(14)))
            '    .tpm = Math.Max(.tpm, CDbl(data(15)))
            '    .tpa = Math.Max(.tpa, CDbl(data(16)))
            '    .tpp = Math.Max(.tpp, CDbl(data(17)))
            '    .ftm = Math.Max(.ftm, CDbl(data(18)))
            '    .fta = Math.Max(.fta, CDbl(data(19)))
            '    .ftp = Math.Max(.ftp, CDbl(data(20)))
            '    .reb = Math.Max(.reb, CDbl(data(21)))
            '    .ast = Math.Max(.ast, CDbl(data(22)))
            '    .blk = Math.Max(.blk, CDbl(data(23)))
            '    .stl = Math.Max(.stl, CDbl(data(24)))
            '    .pf = Math.Max(.pf, CDbl(data(25)))
            '    .too = Math.Max(.too, CDbl(data(26)))
            '    .pts = Math.Max(.pts, CDbl(data(27)))
            '    .fp = Math.Max(.fp, CDbl(data(28)))
            'End With

            If ki >= 0 Then
                averages.SetByIndex(ki, ps)
            Else
                averages.Add(data(0), ps)
            End If

        End While

        sr.Close()
        sr.Dispose()

        Dim keys As New ArrayList

        For Each k As String In averages.Keys
            keys.Add(k)
        Next

        Dim psa As New player_stats
        psa.fpp = New Double(90) {}
        psa.mp = New Double(50) {}

        For j As Integer = 0 To keys.Count - 1
            psa = averages(keys(j))
            With psa
                .minutes /= .count
                .fgm /= .count
                .fga /= .count
                .fgp /= .count
                .tpm /= .count
                .tpa /= .count
                .tpp /= .count
                .ftm /= .count
                .fta /= .count
                .ftp /= .count
                .reb /= .count
                .ast /= .count
                .blk /= .count
                .stl /= .count
                .pf /= .count
                .too /= .count
                .pts /= .count
                .fp /= .count

                'max_average_stats.minutes = Math.Max(max_average_stats.minutes, .minutes)
                'max_average_stats.fgm = Math.Max(max_average_stats.fgm, .fgm)
                'max_average_stats.fga = Math.Max(max_average_stats.fga, .fga)
                'max_average_stats.fgp = Math.Max(max_average_stats.fgp, .fgp)
                'max_average_stats.tpm = Math.Max(max_average_stats.tpm, .tpm)
                'max_average_stats.tpa = Math.Max(max_average_stats.tpa, .tpa)
                'max_average_stats.tpp = Math.Max(max_average_stats.tpp, .tpp)
                'max_average_stats.ftm = Math.Max(max_average_stats.ftm, .ftm)
                'max_average_stats.fta = Math.Max(max_average_stats.fta, .fta)
                'max_average_stats.ftp = Math.Max(max_average_stats.ftp, .ftp)
                'max_average_stats.reb = Math.Max(max_average_stats.reb, .reb)
                'max_average_stats.ast = Math.Max(max_average_stats.ast, .ast)
                'max_average_stats.blk = Math.Max(max_average_stats.blk, .blk)
                'max_average_stats.stl = Math.Max(max_average_stats.stl, .stl)
                'max_average_stats.pf = Math.Max(max_average_stats.pf, .pf)
                'max_average_stats.too = Math.Max(max_average_stats.too, .too)
                'max_average_stats.pts = Math.Max(max_average_stats.pts, .pts)
                'max_average_stats.fp = Math.Max(max_average_stats.fp, .fp)

                Dim total As Double = 0
                For i As Integer = 0 To .fpp.Length - 1
                    total += .fpp(i)
                Next
                If total > 0 Then
                    For i As Integer = 0 To .fpp.Length - 1
                        .fpp(i) /= total
                    Next
                End If

                total = 0
                For i As Integer = 0 To .mp.Length - 1
                    total += .mp(i)
                Next
                If total > 0 Then
                    For i As Integer = 0 To .mp.Length - 1
                        .mp(i) /= total
                    Next
                End If
            End With

            averages(keys(j)) = psa
        Next

    End Sub


    Private Sub getAverages3(ByVal raw_data As String)
        ' Name          Position    Team ID     Team Name               #   Height  WEIGHT  SEASON  DATE        LOCATION    OPP
        ' Josh Smith	SF	        LAC	        Los Angeles Clippers	5	81	    225	    POST	Wed 5/27	AWAY	    GS	   
        '
        ' Minutes   FGM FGA FG%     3PM 3PA 3P%     FTM FTA FT%     REB AST BLK STL PF  TO  PTS FP
        ' 21	    3	14	0.214	2	7	0.286	3	6	0.5	    4	1	2	0	2	0	11	22.5																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																			


        Dim sr As New System.IO.StreamReader(raw_data)

        Dim dupe_count As Integer = 0
        Dim total_count As Integer = 0

        While (Not sr.EndOfStream)

            Dim data() As String = sr.ReadLine.Split(",")

            ' If minutes = 0 and score = 0, the guy didn't play
            If CDbl(data(11)) = 0 And CDbl(data(28)) = 0 Then Continue While

            Dim ps As New player_stats
            ps.fpp = New Double(23) {}
            ps.mp = New Double(13) {}

            Dim ki As Integer = averages.IndexOfKey(data(0))

            If ki >= 0 Then ps = averages.GetByIndex(ki)

            Dim dt As Date
            Try
                dt = Date.Parse(data(8) & "/2015")
            Catch ex As Exception
                Try
                    dt = Date.Parse(data(8) & "/2014")
                Catch ex2 As Exception
                    Try
                        dt = Date.Parse(data(8) & "/2013")
                    Catch ex3 As Exception
                        Try
                            dt = Date.Parse(data(8) & "/2012")
                        Catch ex4 As Exception
                            MsgBox("Can't parse date: " & data(8))
                        End Try
                    End Try
                End Try
            End Try

            'Dim dtk As Integer = scores_by_date.IndexOfKey(dt.ToShortDateString)

            'If dtk >= 0 Then
            '    Dim mm() As Double = scores_by_date.GetByIndex(dtk)
            '    If CDbl(data(28)) < mm(0) Then mm(0) = CDbl(data(28))
            '    If CDbl(data(28)) > mm(1) Then mm(1) = CDbl(data(28))
            '    scores_by_date.SetByIndex(dtk, mm)
            'Else
            '    Dim mm(1) As Double
            '    mm(0) = CDbl(data(28))
            '    mm(1) = CDbl(data(28))
            '    scores_by_date.Add(dt.ToShortDateString, mm)
            'End If

            Dim dtk As Integer = scores_by_player.IndexOfKey(data(0))

            If dtk >= 0 Then
                Dim sl As SortedList = scores_by_player.GetByIndex(dtk)
                Try
                    Dim ss() As Double = New Double() {CDbl(data(28)), 0, CDbl(data(11))}
                    sl.Add(dt.ToShortDateString, ss)
                Catch ex As Exception
                    dupe_count += 1
                    Continue While
                    'Dim sc As Double = sl(dt.ToShortDateString)
                    'MsgBox("Error adding date: " & data(0) & vbTab & dt.ToShortDateString & vbTab & data(28) & vbCrLf & sc)
                End Try
                scores_by_player.SetByIndex(dtk, sl)
            Else
                Try
                    Dim sl As New SortedList
                    Dim ss() As Double = New Double() {CDbl(data(28)), 0, CDbl(data(11))}
                    sl.Add(dt.ToShortDateString, ss)
                    scores_by_player.Add(data(0), sl)
                Catch ex As Exception
                    MsgBox("Error adding date: " & data(0) & vbTab & dt.ToShortDateString & vbTab & data(28))
                End Try
            End If

            With ps
                .name = data(0)
                .position = data(1)
                .team_ID = data(2)
                .team_name = data(3)
                .height = CInt(data(5))
                .weight = CInt(data(6))
                .minutes += CDbl(data(11))
                .fgm += CDbl(data(12))
                .fga += CDbl(data(13))
                .fgp += CDbl(data(14))
                .tpm += CDbl(data(15))
                .tpa += CDbl(data(16))
                .tpp += CDbl(data(17))
                .ftm += CDbl(data(18))
                .fta += CDbl(data(19))
                .ftp += CDbl(data(20))
                .reb += CDbl(data(21))
                .ast += CDbl(data(22))
                .blk += CDbl(data(23))
                .stl += CDbl(data(24))
                .pf += CDbl(data(25))
                .too += CDbl(data(26))
                .pts += CDbl(data(27))
                .fp += CDbl(data(28))
                .fpp(Math.Max(0, Math.Min(.fpp.Length - 1, Math.Round(CDbl(data(28)) / 4)))) += 1
                .mp(Math.Max(0, Math.Min(.mp.Length - 1, Math.Round(CDbl(data(11)) / 4)))) += 1
                .count += 1
            End With

            'With max_stats
            '    .height = Math.Max(.height, CInt(data(5)))
            '    .weight = Math.Max(.weight, CInt(data(6)))
            '    .minutes = Math.Max(.minutes, CDbl(data(11)))
            '    .fgm = Math.Max(.fgm, CDbl(data(12)))
            '    .fga = Math.Max(.fga, CDbl(data(13)))
            '    .fgp = Math.Max(.fgp, CDbl(data(14)))
            '    .tpm = Math.Max(.tpm, CDbl(data(15)))
            '    .tpa = Math.Max(.tpa, CDbl(data(16)))
            '    .tpp = Math.Max(.tpp, CDbl(data(17)))
            '    .ftm = Math.Max(.ftm, CDbl(data(18)))
            '    .fta = Math.Max(.fta, CDbl(data(19)))
            '    .ftp = Math.Max(.ftp, CDbl(data(20)))
            '    .reb = Math.Max(.reb, CDbl(data(21)))
            '    .ast = Math.Max(.ast, CDbl(data(22)))
            '    .blk = Math.Max(.blk, CDbl(data(23)))
            '    .stl = Math.Max(.stl, CDbl(data(24)))
            '    .pf = Math.Max(.pf, CDbl(data(25)))
            '    .too = Math.Max(.too, CDbl(data(26)))
            '    .pts = Math.Max(.pts, CDbl(data(27)))
            '    .fp = Math.Max(.fp, CDbl(data(28)))
            'End With

            If ki >= 0 Then
                averages.SetByIndex(ki, ps)
            Else
                averages.Add(data(0), ps)
            End If

            total_count += 1

        End While

        sr.Close()
        sr.Dispose()

        Dim keys As New ArrayList

        For Each k As String In averages.Keys
            keys.Add(k)
        Next

        Dim psa As New player_stats
        psa.fpp = New Double(23) {}
        psa.mp = New Double(13) {}

        For j As Integer = 0 To keys.Count - 1
            psa = averages(keys(j))
            With psa
                .minutes /= .count
                .fgm /= .count
                .fga /= .count
                .fgp /= .count
                .tpm /= .count
                .tpa /= .count
                .tpp /= .count
                .ftm /= .count
                .fta /= .count
                .ftp /= .count
                .reb /= .count
                .ast /= .count
                .blk /= .count
                .stl /= .count
                .pf /= .count
                .too /= .count
                .pts /= .count
                .fp /= .count

                'max_average_stats.minutes = Math.Max(max_average_stats.minutes, .minutes)
                'max_average_stats.fgm = Math.Max(max_average_stats.fgm, .fgm)
                'max_average_stats.fga = Math.Max(max_average_stats.fga, .fga)
                'max_average_stats.fgp = Math.Max(max_average_stats.fgp, .fgp)
                'max_average_stats.tpm = Math.Max(max_average_stats.tpm, .tpm)
                'max_average_stats.tpa = Math.Max(max_average_stats.tpa, .tpa)
                'max_average_stats.tpp = Math.Max(max_average_stats.tpp, .tpp)
                'max_average_stats.ftm = Math.Max(max_average_stats.ftm, .ftm)
                'max_average_stats.fta = Math.Max(max_average_stats.fta, .fta)
                'max_average_stats.ftp = Math.Max(max_average_stats.ftp, .ftp)
                'max_average_stats.reb = Math.Max(max_average_stats.reb, .reb)
                'max_average_stats.ast = Math.Max(max_average_stats.ast, .ast)
                'max_average_stats.blk = Math.Max(max_average_stats.blk, .blk)
                'max_average_stats.stl = Math.Max(max_average_stats.stl, .stl)
                'max_average_stats.pf = Math.Max(max_average_stats.pf, .pf)
                'max_average_stats.too = Math.Max(max_average_stats.too, .too)
                'max_average_stats.pts = Math.Max(max_average_stats.pts, .pts)
                'max_average_stats.fp = Math.Max(max_average_stats.fp, .fp)

                Dim total As Double = 0
                For i As Integer = 0 To .fpp.Length - 1
                    total += .fpp(i)
                Next
                If total > 0 Then
                    For i As Integer = 0 To .fpp.Length - 1
                        .fpp(i) /= total
                    Next
                End If

                total = 0
                For i As Integer = 0 To .mp.Length - 1
                    total += .mp(i)
                Next
                If total > 0 Then
                    For i As Integer = 0 To .mp.Length - 1
                        .mp(i) /= total
                    Next
                End If
            End With

            averages(keys(j)) = psa
        Next


        'GID	Name	        Date	    Team	Opp	H/A	Start	Minutes	GP	obsolete	FDP	S   FP	    obsolete	obsolete	
        '4699	Payton, Elfrid	20150415	orl	    bkn	A	1	    35	    1	24	        20.6	17.75	20.75	    20.75	

        'DDP	DKP	    obsolete	blank	Stats	                            FD Sal	DD Sal	DK Sal	SF Sal	blank	FD pos	
        '21.25	22.75	20.25		        2pt 3rb 6as 4st 1bl 4to 1-5fg 0-2ft	7900	12550	7600	13415		    1	    

        'DD pos	DK pos	SF pos	Team pts	Opp pts
        '1	    1	    G	    88	        101

        Dim otherdata() As String = System.IO.File.ReadAllLines("C:\Users\OK\Desktop\FF\summary.txt")

        Dim missing_players As Integer = 0
        Dim missing_dates As Integer = 0

        For i As Integer = 1 To otherdata.Length - 1
            Dim ds() As String = otherdata(i).Split(":")

            Dim salary As Double = 0
            If ds(21) <> "" And ds(21) <> "" Then salary = CDbl(ds(21).Trim(" "))

            If salary = 0 Then Continue For


            Dim name As String = ds(1).Split(",")(1).Replace(" ", "") & " " & ds(1).Split(",")(0).Replace(" ", "")
            Dim dt As Date = Date.Parse(ds(2)(4) & ds(2)(5) & "/" & ds(2)(6) & ds(2)(7) & "/" & ds(2)(0) & ds(2)(1) & ds(2)(2) & ds(2)(3))

            Dim played As Integer = CInt(ds(8))

            Dim dtk As Integer = scores_by_player.IndexOfKey(name)

            If dtk >= 0 Then
                Dim sl As SortedList = scores_by_player.GetByIndex(dtk)
                Dim sld As Integer = sl.IndexOfKey(dt.ToShortDateString)
                If sld >= 0 Then
                    Dim ss() As Double = sl.GetByIndex(sld)
                    ss(1) = salary
                    sl.SetByIndex(sld, ss)
                    scores_by_player.SetByIndex(dtk, sl)
                ElseIf played > 0 Then
                    missing_dates += 1
                    'Dim output As String = ""
                    'For Each k As String In sl.Keys
                    '    Dim ss() As Double = sl(k)
                    '    output &= k & vbTab & ss(0) & vbTab & ss(1) & vbCrLf
                    'Next
                    'MsgBox("No date found: " & name & vbTab & ds(2) & vbTab & played & vbCrLf & output)
                End If
            Else
                missing_players += 1
                'MsgBox("No player found: " & name)
            End If

        Next

        'MsgBox(missing_dates & " / " & missing_players & " / " & otherdata.Length - 1)

    End Sub

    Private Function getLastNScores(ByVal name As String, ByVal nscores As Integer, ByVal current_date As Date, _
                                    ByRef sbp As SortedList) As Double()

        Try

            Dim si As Integer = sbp.IndexOfKey(name)

            If si >= 0 Then

                Dim sl As SortedList = sbp.GetByIndex(si)

                Dim dts(sl.Count - 1) As Date
                Dim scores(sl.Count - 1) As Double
                Dim minutes(sl.Count - 1) As Double

                Dim count As Integer = 0
                For Each k As String In sl.Keys
                    dts(count) = Date.Parse(k)
                    scores(count) = sl(k)(0)
                    minutes(count) = sl(k)(2)
                    count += 1
                Next

                Array.Sort(dts, scores)

                'Dim output2 As String = ""
                'For i As Integer = 0 To dts.Length - 1
                '    output2 &= dts(i).ToShortDateString & vbTab & scores(i) & vbCrLf
                'Next

                Dim start As Integer = -1
                For i As Integer = 0 To dts.Length - 1
                    If Date.Compare(current_date, dts(i)) = 0 Then
                        start = i - 1
                        Exit For
                    ElseIf Date.Compare(current_date, dts(i)) < 0 Then
                        start = i
                        Exit For
                    End If
                Next

                Dim ls(nscores * 3 - 1) As Double

                For i As Integer = 0 To ls.Length - 1
                    ls(i) = -1
                Next

                If start < 0 Then Return ls

                Try

                    'Dim fc As Integer = 0
                    'Dim output As String = name & vbTab & current_date.ToShortDateString & vbTab & dts.Length & vbCrLf
                    For i As Integer = start To Math.Max(0, start - nscores + 1) Step -1
                        ls(start - i) = scores(i) / 100
                        ls(nscores + (start - i)) = current_date.Subtract(dts(i)).Days() / 300
                        ls(nscores * 2 + (start - i)) = minutes(i) / 100
                        'output &= dts(i).ToShortDateString & vbTab & ls(i - start) & vbCrLf
                        'fc += 1
                    Next
                Catch ex2 As Exception
                    MsgBox("Here: " & ex2.Message & vbCrLf & start & vbCrLf & Math.Max(0, start - nscores) & vbCrLf & dts.Length)
                    Dim output As String = ""
                    For i As Integer = start To Math.Max(0, start - nscores) Step -1
                        output &= start - i & vbTab & i & vbTab & scores.Length & vbCrLf
                    Next
                    MsgBox(output)
                End Try

                'If fc > 5 Then MsgBox(output & vbCrLf & vbCrLf & output2)

                Return ls

            Else
                MsgBox("Couldn't find name: " & name)
            End If

        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

        Dim tls(nscores - 1) As Double

        For i As Integer = 0 To tls.Length - 1
            tls(i) = -1
        Next

        Return tls

    End Function


    Private Function getInputs2(ByVal player As String, ByVal season As String, ByVal home As Boolean, ByVal gamedate As Date, _
                               ByVal opp_ID As String, Optional ByVal player_position As String = "") As Double()

        ' Need to check: MCH, EAST, FLMG, MTV, ALB
        Static team_IDs_alt() As String = {"LAC", "GS", "TOR", "CLE", "DAL", "ATL", "SA", "POR", "NOP", "CHI", "OKC", "HOU", "MEM", "PHO", "UTAH", "SAC", "DET", "BKN", "WSH", "BOS", "DEN", "MIA", "LAL", "IND", "MIL", "MIN", "ORL", "CHA", "NY", "PHI"}
        Static team_IDs() As String = {"LAC", "GSW", "TOR", "CLE", "DAL", "ATL", "SAS", "POR", "NO", "CHI", "OKC", "HOU", "MEM", "PHX", "UTA", "SAC", "DET", "BKN", "WAS", "BOS", "DEN", "MIA", "LAL", "IND", "MIL", "MIN", "ORL", "CHA", "NYK", "PHI"}
        Static team_names() As String = {"LA Clippers", "Golden State", "Toronto", "Cleveland", "Dallas", "Atlanta", "San Antonio", "Portland", "New Orleans", "Chicago", "Oklahoma City", "Houston", "Memphis", "Phoenix", "Utah", "Sacramento", "Detroit", "Brooklyn", "Washington", "Boston", "Denver", "Miami", "LA Lakers", "Indiana", "Milwaukee", "Minnesota", "Orlando", "Charlotte", "New York", "Philadelphia"}
        Static max() As Double = {86, 307, 55, 21, 43, 1, 11, 19, 1, 22, 34, 1, 27, 21, 12, 8, 6, 12, 57, 100}

        ' Name          Position    Team ID     Team Name               #   Height  WEIGHT  SEASON  DATE        LOCATION    OPP
        ' Josh Smith	SF	        LAC	        Los Angeles Clippers	5	81	    225	    POST	Wed 5/27	AWAY	    GS	   
        '
        ' Minutes   FGM FGA FG%     3PM 3PA 3P%     FTM FTA FT%     REB AST BLK STL PF  TO  PTS FP
        ' 21	    3	14	0.214	2	7	0.286	3	6	0.5	    4	1	2	0	2	0	11	22.5																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																			


        Dim pi As Integer = averages.IndexOfKey(player)

        If pi < 0 Then
            MsgBox("Error locating: " & player)
            Return Nothing
        End If

        Dim ps As player_stats = averages.GetByIndex(pi)



        Try

            Dim inputs As New ArrayList

            Dim pa(4) As Double

            If player_position = "" Then
                Select Case ps.position
                    Case "PF" : pa(0) = 1 : Exit Select
                    Case "PG" : pa(1) = 1 : Exit Select
                    Case "SF" : pa(2) = 1 : Exit Select
                    Case "SG" : pa(3) = 1 : Exit Select
                    Case "C" : pa(4) = 1 : Exit Select
                    Case Else : MsgBox("Unknown position: " & ps.position)
                End Select
            Else
                Select Case player_position
                    Case "PF" : pa(0) = 1 : Exit Select
                    Case "PG" : pa(1) = 1 : Exit Select
                    Case "SF" : pa(2) = 1 : Exit Select
                    Case "SG" : pa(3) = 1 : Exit Select
                    Case "C" : pa(4) = 1 : Exit Select
                    Case Else : MsgBox("Unknown position: " & ps.position)
                End Select
            End If

            inputs.AddRange(pa)


            Dim sea(2) As Double
            Select Case season
                Case "POST" : sea(0) = 1 : Exit Select
                Case "PRE" : sea(1) = 1 : Exit Select
                Case "REGULAR" : sea(2) = 1 : Exit Select
                Case Else : MsgBox("Unknown season: " & season)
            End Select

            inputs.AddRange(sea)


            If home Then inputs.Add(1) Else inputs.Add(-1)


            'Wed 5/27
            Dim day(6) As Double
            day(gamedate.DayOfWeek) = 1

            inputs.AddRange(day)


            Dim month(11) As Double
            month(gamedate.Month - 1) = 1

            inputs.AddRange(month)


            With ps
                inputs.Add(.height / max(0))
                inputs.Add(.weight / max(1))
                inputs.Add(.minutes / max(2))
                inputs.Add(.fgm / max(3))
                inputs.Add(.fga / max(4))
                inputs.Add(.fgp / max(5))
                inputs.Add(.tpm / max(6))
                inputs.Add(.tpa / max(7))
                inputs.Add(.tpp / max(8))
                inputs.Add(.ftm / max(9))
                inputs.Add(.fta / max(10))
                inputs.Add(.ftp / max(11))
                inputs.Add(.reb / max(12))
                inputs.Add(.ast / max(13))
                inputs.Add(.blk / max(14))
                inputs.Add(.stl / max(15))
                inputs.Add(.pf / max(16))
                inputs.Add(.too / max(17))
                inputs.Add(.pts / max(18))
                inputs.Add(.fp / max(19))
                inputs.Add(Math.Min(1, .count / 100))
                inputs.AddRange(.fpp)
                inputs.AddRange(.mp)
            End With

            Dim ts() As Double

            ' Player team data
            Dim i As Integer = teams.IndexOfKey(ps.team_ID)
            If i < 0 Then
                Dim j As Integer = Array.IndexOf(team_IDs_alt, ps.team_ID)
                If j >= 0 Then i = teams.IndexOfKey(team_IDs(j))
                If i < 0 Then
                    'MsgBox("No team ID: " & data(2))
                    'Throw New Exception("no team")
                    Try
                        System.IO.File.AppendAllText("missing_teams.txt", ps.team_ID & vbCrLf)
                    Catch ex As Exception
                    End Try
                    ts = New Double() {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}
                Else
                    ts = teams.GetByIndex(i)
                End If
            Else
                ts = teams.GetByIndex(i)
            End If


            inputs.AddRange(ts)


            ' Opponent team data
            i = teams.IndexOfKey(opp_ID)
            If i < 0 Then
                Dim j As Integer = Array.IndexOf(team_IDs_alt, opp_ID)
                If j >= 0 Then i = teams.IndexOfKey(team_IDs(j))
                If i < 0 Then
                    Try
                        System.IO.File.AppendAllText("missing_opponents.txt", opp_ID & vbCrLf)
                    Catch ex As Exception
                    End Try
                    ts = New Double() {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}
                Else
                    ts = teams.GetByIndex(i)
                End If
            Else
                ts = teams.GetByIndex(i)
            End If

            inputs.AddRange(ts)

            'Dim sinputs As String = ""

            'For i = 0 To inputs.Count - 1
            '    sinputs &= inputs(i)
            '    If i < inputs.Count - 1 Then sinputs &= ","
            'Next

            Dim dinputs(inputs.Count - 1) As Double

            For j As Integer = 0 To inputs.Count - 1
                dinputs(j) = inputs(j)
            Next

            Return dinputs

        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

        Return Nothing

    End Function

    Private Function getInputs3(ByVal player As String, ByVal season As String, ByVal home As Boolean, ByVal gamedate As Date, _
                               ByVal opp_ID As String, Optional ByVal player_position As String = "") As Double()

        ' Need to check: MCH, EAST, FLMG, MTV, ALB
        Static team_IDs_alt() As String = {"LAC", "GS", "TOR", "CLE", "DAL", "ATL", "SA", "POR", "NOP", "CHI", "OKC", "HOU", "MEM", "PHO", "UTAH", "SAC", "DET", "BKN", "WSH", "BOS", "DEN", "MIA", "LAL", "IND", "MIL", "MIN", "ORL", "CHA", "NY", "PHI"}
        Static team_IDs() As String = {"LAC", "GSW", "TOR", "CLE", "DAL", "ATL", "SAS", "POR", "NO", "CHI", "OKC", "HOU", "MEM", "PHX", "UTA", "SAC", "DET", "BKN", "WAS", "BOS", "DEN", "MIA", "LAL", "IND", "MIL", "MIN", "ORL", "CHA", "NYK", "PHI"}
        Static team_names() As String = {"LA Clippers", "Golden State", "Toronto", "Cleveland", "Dallas", "Atlanta", "San Antonio", "Portland", "New Orleans", "Chicago", "Oklahoma City", "Houston", "Memphis", "Phoenix", "Utah", "Sacramento", "Detroit", "Brooklyn", "Washington", "Boston", "Denver", "Miami", "LA Lakers", "Indiana", "Milwaukee", "Minnesota", "Orlando", "Charlotte", "New York", "Philadelphia"}
        Static max() As Double = {86, 307, 55, 21, 43, 1, 11, 19, 1, 22, 34, 1, 27, 21, 12, 8, 6, 12, 57, 100}

        ' Name          Position    Team ID     Team Name               #   Height  WEIGHT  SEASON  DATE        LOCATION    OPP
        ' Josh Smith	SF	        LAC	        Los Angeles Clippers	5	81	    225	    POST	Wed 5/27	AWAY	    GS	   
        '
        ' Minutes   FGM FGA FG%     3PM 3PA 3P%     FTM FTA FT%     REB AST BLK STL PF  TO  PTS FP
        ' 21	    3	14	0.214	2	7	0.286	3	6	0.5	    4	1	2	0	2	0	11	22.5																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																			


        Dim pi As Integer = averages.IndexOfKey(player)

        If pi < 0 Then
            MsgBox("Error locating: " & player)
            Return Nothing
        End If

        Dim ps As player_stats = averages.GetByIndex(pi)



        Try

            Dim inputs As New ArrayList

            Dim pa(4) As Double

            If player_position = "" Then
                Select Case ps.position
                    Case "PF" : pa(0) = 1 : Exit Select
                    Case "PG" : pa(1) = 1 : Exit Select
                    Case "SF" : pa(2) = 1 : Exit Select
                    Case "SG" : pa(3) = 1 : Exit Select
                    Case "C" : pa(4) = 1 : Exit Select
                    Case Else : MsgBox("Unknown position: " & ps.position)
                End Select
            Else
                Select Case player_position
                    Case "PF" : pa(0) = 1 : Exit Select
                    Case "PG" : pa(1) = 1 : Exit Select
                    Case "SF" : pa(2) = 1 : Exit Select
                    Case "SG" : pa(3) = 1 : Exit Select
                    Case "C" : pa(4) = 1 : Exit Select
                    Case Else : MsgBox("Unknown position: " & ps.position)
                End Select
            End If

            inputs.AddRange(pa)


            Dim sea(2) As Double
            Select Case season
                Case "POST" : sea(0) = 1 : Exit Select
                Case "PRE" : sea(1) = 1 : Exit Select
                Case "REGULAR" : sea(2) = 1 : Exit Select
                Case Else : MsgBox("Unknown season: " & season)
            End Select

            inputs.AddRange(sea)


            If home Then inputs.Add(1) Else inputs.Add(-1)


            'Wed 5/27
            Dim day(6) As Double
            day(gamedate.DayOfWeek) = 1

            inputs.AddRange(day)


            Dim month(11) As Double
            month(gamedate.Month - 1) = 1

            inputs.AddRange(month)

            Dim year(5) As Double
            year(2016 - gamedate.Year) = 1

            inputs.AddRange(year)


            'inputs.Add(Math.Min(1, getNDaysLastPlayed(player, gamedate, scores_by_player) / 100))


            With ps
                inputs.Add(.height / max(0))
                inputs.Add(.weight / max(1))
                inputs.Add(.minutes / max(2))
                inputs.Add(.fgm / max(3))
                inputs.Add(.fga / max(4))
                inputs.Add(.fgp / max(5))
                inputs.Add(.tpm / max(6))
                inputs.Add(.tpa / max(7))
                inputs.Add(.tpp / max(8))
                inputs.Add(.ftm / max(9))
                inputs.Add(.fta / max(10))
                inputs.Add(.ftp / max(11))
                inputs.Add(.reb / max(12))
                inputs.Add(.ast / max(13))
                inputs.Add(.blk / max(14))
                inputs.Add(.stl / max(15))
                inputs.Add(.pf / max(16))
                inputs.Add(.too / max(17))
                inputs.Add(.pts / max(18))
                inputs.Add(.fp / max(19))
                inputs.Add(Math.Min(1, .count / 100))
                inputs.AddRange(.fpp)
                inputs.AddRange(.mp)
            End With

            Dim ts() As Double

            ' Player team data
            Dim i As Integer = teams.IndexOfKey(ps.team_ID)
            If i < 0 Then
                Dim j As Integer = Array.IndexOf(team_IDs_alt, ps.team_ID)
                If j >= 0 Then i = teams.IndexOfKey(team_IDs(j))
                If ps.team_ID = "NOR" Then i = teams.IndexOfKey("NO")
                If i < 0 Then
                    'MsgBox("No team ID: " & data(2))
                    'Throw New Exception("no team")
                    Try
                        System.IO.File.AppendAllText("missing_teams.txt", ps.team_ID & vbCrLf)
                    Catch ex As Exception
                    End Try
                    ts = New Double() {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}
                Else
                    ts = teams.GetByIndex(i)
                End If
            Else
                ts = teams.GetByIndex(i)
            End If


            inputs.AddRange(ts)


            ' Opponent team data
            i = teams.IndexOfKey(opp_ID)
            If i < 0 Then
                Dim j As Integer = Array.IndexOf(team_IDs_alt, opp_ID)
                If j >= 0 Then i = teams.IndexOfKey(team_IDs(j))
                If opp_ID = "NOR" Then i = teams.IndexOfKey("NO")
                If i < 0 Then
                    Try
                        System.IO.File.AppendAllText("missing_opponents.txt", opp_ID & vbCrLf)
                    Catch ex As Exception
                    End Try
                    ts = New Double() {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}
                Else
                    ts = teams.GetByIndex(i)
                End If
            Else
                ts = teams.GetByIndex(i)
            End If

            inputs.AddRange(ts)

            Dim lsc() As Double = getLastNScores(player, 10, gamedate, scores_by_player)

            inputs.AddRange(lsc)

            'Dim sinputs As String = ""

            'For i = 0 To inputs.Count - 1
            '    sinputs &= inputs(i)
            '    If i < inputs.Count - 1 Then sinputs &= ","
            'Next

            Dim dinputs(inputs.Count - 1) As Double

            For j As Integer = 0 To inputs.Count - 1
                dinputs(j) = inputs(j)
            Next

            Return dinputs

        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

        Return Nothing

    End Function

    Private Sub applyModel()

        Dim data(ListView1.Items.Count - 1) As String

        For i As Integer = 0 To ListView1.Items.Count - 1
            data(i) = "" 'ListView1.Items(i).Text & ","
            If Not ListView1.Items(i).Checked Then Continue For
            For j As Integer = 0 To ListView1.Items(i).SubItems.Count - 1
                data(i) &= ListView1.Items(i).SubItems(j).Text
                If j < ListView1.Items(i).SubItems.Count - 1 Then data(i) &= ","
            Next
        Next

        For i As Integer = 0 To data.Length - 1
            If data(i) <> "" Then

                Dim ds() As String = data(i).Split(",")
                Dim pi As Integer = averages.IndexOfKey(ds(0))
                If pi < 0 Then
                    ds(4) = "0"
                    data(i) = ""
                    For j As Integer = 0 To ds.Length - 1
                        data(i) &= ds(j)
                        If j < ds.Length - 1 Then data(i) &= ","
                    Next
                    Continue For
                    MsgBox("Error finding player: " & i & vbTab & ds(1))
                End If

                Dim ps As player_stats = averages.GetByIndex(pi)

                Dim home As Boolean = ds(3).Split(" ")(0).Split("@")(1).ToUpper = ds(5).ToUpper
                Dim opp_ID As String = ""

                If home Then
                    opp_ID = ds(3).Split(" ")(0).Split("@")(0).ToUpper
                Else
                    opp_ID = ds(3).Split(" ")(0).Split("@")(1).ToUpper
                End If

                Dim inputs() As Double = getInputs3(ds(0), CStr(ComboBox1.Items(ComboBox1.SelectedIndex)), _
                                                   home, DateTimePicker1.Value, opp_ID, ds(1))

                Dim output(0) As Double

                If model Is Nothing Then
                    Dim tin As New Encog.ML.Data.Basic.BasicMLData(inputs)
                    Dim teo As Encog.ML.Data.IMLData = model2.Compute(tin)
                    output = teo.Data
                Else
                    model.Compute(inputs, output)
                End If

                'Dim salary As Double = CDbl(ds(2))

                'Dim weight As Double = Math.Sqrt(salary / 10500)

                'Dim avg As Double = CDbl(ds(4))

                ds(4) = (output(0) * 100).ToString("N4")

                data(i) = ""
                For j As Integer = 0 To ds.Length - 1
                    data(i) &= ds(j)
                    If j < ds.Length - 1 Then data(i) &= ","
                Next
            End If
        Next

        ListView3.Items.Clear()

        For i As Integer = 0 To data.Length - 1
            If data(i) <> "" Then
                Dim ds() As String = data(i).Split(",")
                Dim lvi As New ListViewItem()
                lvi.Text = ds(0)
                For j As Integer = 1 To ds.Length - 1
                    lvi.SubItems.Add(ds(j))
                Next
                lvi.Checked = True
                ListView3.Items.Add(lvi)
            End If
        Next

    End Sub



    Public Sub loadData()

        pg.Clear()
        sg.Clear()
        sf.Clear()
        pf.Clear()
        c.Clear()
        g.Clear()
        f.Clear()
        u.Clear()

        For i As Integer = 0 To ListView3.Items.Count - 1

            Dim pi As New player_info
            pi.name = ListView3.Items(i).SubItems(0).Text
            pi.position = ListView3.Items(i).SubItems(1).Text
            pi.salary = CDbl(ListView3.Items(i).SubItems(2).Text)
            pi.game_info = ListView3.Items(i).SubItems(3).Text
            pi.avg = CDbl(ListView3.Items(i).SubItems(4).Text)
            pi.team = ListView3.Items(i).SubItems(5).Text

            Select Case pi.position.ToUpper
                Case "PG"
                    pg.Add(pi)
                    g.Add(pi)
                    Exit Select
                Case "SG"
                    sg.Add(pi)
                    g.Add(pi)
                    Exit Select
                Case "SF"
                    sf.Add(pi)
                    f.Add(pi)
                    Exit Select
                Case "PF"
                    pf.Add(pi)
                    f.Add(pi)
                    Exit Select
                Case "C"
                    c.Add(pi)
                    Exit Select
            End Select

            u.Add(pi)

        Next

    End Sub

    Public Class FFScore : Implements ICalculateGenomeScore

        Public Function CalculateScore(ByVal genome As Encog.ML.Genetic.Genome.IGenome) As Double Implements Encog.ML.Genetic.Genome.ICalculateGenomeScore.CalculateScore
            Const max_salary As Double = 50000

            Dim organism() As Integer = genome.Organism()

            ' Can replace these with projections for the players once we have a model.
            Dim pi_pg As player_info = pg(organism(0))
            Dim pi_sg As player_info = sg(organism(1))
            Dim pi_sf As player_info = sf(organism(2))
            Dim pi_pf As player_info = pf(organism(3))
            Dim pi_c As player_info = c(organism(4))
            Dim pi_g As player_info = g(organism(5))
            Dim pi_f As player_info = f(organism(6))
            Dim pi_u As player_info = u(organism(7))

            Dim problem As Boolean = True

            If pi_pg.name <> pi_g.name And pi_sg.name <> pi_g.name And _
               pi_sf.name <> pi_f.name And pi_pf.name <> pi_f.name And _
               pi_u.name <> pi_pg.name And pi_u.name <> pi_sg.name And _
               pi_u.name <> pi_sf.name And pi_u.name <> pi_pf.name And _
               pi_u.name <> pi_c.name And pi_u.name <> pi_g.name And _
               pi_u.name <> pi_f.name Then problem = False

            'If (Not problem) And exclude_best And best_organism IsNot Nothing Then

            '    Dim names(7) As String
            '    names(0) = pi_pg.name
            '    names(1) = pi_sg.name
            '    names(2) = pi_sf.name
            '    names(3) = pi_pf.name
            '    names(4) = pi_c.name
            '    names(5) = pi_g.name
            '    names(6) = pi_f.name
            '    names(7) = pi_u.name

            '    Dim best_names(7) As String
            '    best_names(0) = CType(pg(best_organism(0)), player_info).name
            '    best_names(1) = CType(sg(best_organism(1)), player_info).name
            '    best_names(2) = CType(sf(best_organism(2)), player_info).name
            '    best_names(3) = CType(pf(best_organism(3)), player_info).name
            '    best_names(4) = CType(c(best_organism(4)), player_info).name
            '    best_names(5) = CType(g(best_organism(5)), player_info).name
            '    best_names(6) = CType(f(best_organism(6)), player_info).name
            '    best_names(7) = CType(u(best_organism(7)), player_info).name

            '    For i As Integer = 0 To organism.Length - 1
            '        For j As Integer = 0 To organism.Length - 1
            '            If names(i) = best_names(j) Then
            '                problem = True
            '                Exit For
            '            End If
            '        Next
            '        If problem Then Exit For
            '    Next

            'End If

            If problem Then Return -1

            Dim total_salary As Double = pi_pg.salary + pi_sg.salary + pi_sf.salary + pi_pf.salary + pi_c.salary + pi_g.salary + pi_f.salary + pi_u.salary

            Dim total_points As Double = pi_pg.avg + pi_sg.avg + pi_sf.avg + pi_pf.avg + pi_c.avg + pi_g.avg + pi_f.avg + pi_u.avg

            If total_salary > max_salary Then Return -1

            Return total_points

        End Function

        Public ReadOnly Property ShouldMinimize() As Boolean Implements Encog.ML.Genetic.Genome.ICalculateGenomeScore.ShouldMinimize
            Get
                Return False
            End Get
        End Property
    End Class

    Public Class FFGenome : Inherits BasicGenome

        Private ReadOnly pathChromosome As Chromosome

        Public Sub New(ByVal owner As GeneticAlgorithm)

            Dim organism(7) As Integer

            organism(0) = rand.Next(0, pg.Count)
            organism(1) = rand.Next(0, sg.Count)
            organism(2) = rand.Next(0, sf.Count)
            organism(3) = rand.Next(0, pf.Count)
            organism(4) = rand.Next(0, c.Count)
            organism(5) = rand.Next(0, g.Count)
            organism(6) = rand.Next(0, f.Count)
            organism(7) = rand.Next(0, u.Count)

            While True

                Dim pi_pg As player_info = pg(organism(0))
                Dim pi_sg As player_info = sg(organism(1))
                Dim pi_sf As player_info = sf(organism(2))
                Dim pi_pf As player_info = pf(organism(3))
                Dim pi_c As player_info = c(organism(4))
                Dim pi_g As player_info = g(organism(5))
                Dim pi_f As player_info = f(organism(6))
                Dim pi_u As player_info = u(organism(7))

                If pi_pg.name <> pi_g.name And pi_sg.name <> pi_g.name And _
                   pi_sf.name <> pi_f.name And pi_pf.name <> pi_f.name And _
                   pi_u.name <> pi_pg.name And pi_u.name <> pi_sg.name And _
                   pi_u.name <> pi_sf.name And pi_u.name <> pi_pf.name And _
                   pi_u.name <> pi_c.name And pi_u.name <> pi_g.name And _
                   pi_u.name <> pi_f.name Then Exit While

                organism(0) = rand.Next(0, pg.Count)
                organism(1) = rand.Next(0, sg.Count)
                organism(2) = rand.Next(0, sf.Count)
                organism(3) = rand.Next(0, pf.Count)
                organism(4) = rand.Next(0, c.Count)
                organism(5) = rand.Next(0, g.Count)
                organism(6) = rand.Next(0, f.Count)
                organism(7) = rand.Next(0, u.Count)

            End While


            pathChromosome = New Chromosome()
            Me.Chromosomes.Add(pathChromosome)

            For i As Integer = 0 To organism.Length - 1
                Dim gene As New IntegerGene
                gene.Value = organism(i)
                pathChromosome.Genes.Add(gene)
            Next

            Me.Organism = organism

            Encode()

        End Sub


        Public Overrides Sub Decode()
            Dim chromosome As Chromosome = Chromosomes(0)
            Dim organism(chromosome.Genes.Count - 1) As Integer
            For i As Integer = 0 To chromosome.Genes.Count - 1
                Dim gene As IntegerGene = CType(chromosome.Genes(i), IntegerGene)
                organism(i) = gene.Value
            Next
            Me.Organism = organism
        End Sub

        Public Overrides Sub Encode()
            Dim organism() As Integer = Me.Organism
            For i As Integer = 0 To Chromosomes(0).Genes.Count - 1
                Dim gene As IntegerGene = CType(Chromosomes(0).Genes(i), IntegerGene)
                gene.Value = organism(i)
                Chromosomes(0).Genes(i) = gene
            Next
        End Sub

    End Class

    Private Sub initPopulation(ByRef ga As GeneticAlgorithm)
        ga.CalculateScore = New FFScore
        ga.Population = New Encog.ML.Genetic.Population.BasicPopulation(POPULATION_SIZE)

        For i As Integer = 0 To POPULATION_SIZE - 1
            Dim genome As New FFGenome(ga)
            ga.Population.Add(genome)
            ga.PerformCalculateScore(genome)
        Next

        ga.Population.Sort()
    End Sub

    Private Sub trainThread(ByVal o As Object)

        If ListView3.Items.Count >= 0 Then

            loadData()

            Dim genetic As New BasicGeneticAlgorithm()

            initPopulation(genetic)
            genetic.MutationPercent = MUTATION_PERCENT
            genetic.PercentToMate = PERCENT_TO_MATE
            genetic.MatingPopulation = MATING_POPULATION_PERCENT
            genetic.Crossover = New Encog.ML.Genetic.Crossover.Splice(3)
            genetic.Mutate = New Encog.ML.Genetic.Mutate.MutatePerturb(0.5)

            Dim sameSolutionCount As Integer = 0
            Dim lastSolution As Double = Double.NegativeInfinity

            Dim count As Integer = 0

            Dim best_score As Double = Double.NegativeInfinity

            While sameSolutionCount < MAX_SAME_SOLUTION

                genetic.Iteration()

                Dim organism() As Integer = genetic.Population.Best.Organism

                If genetic.Population.Best.Score > best_score Then

                    Dim pi_pg As player_info = pg(organism(0))
                    Dim pi_sg As player_info = sg(organism(1))
                    Dim pi_sf As player_info = sf(organism(2))
                    Dim pi_pf As player_info = pf(organism(3))
                    Dim pi_c As player_info = c(organism(4))
                    Dim pi_g As player_info = g(organism(5))
                    Dim pi_f As player_info = f(organism(6))
                    Dim pi_u As player_info = u(organism(7))

                    ListView2.Items.Clear()

                    Dim lvi(8) As ListViewItem

                    lvi(0) = New ListViewItem
                    lvi(0).Text = pi_pg.name
                    lvi(0).SubItems.Add(pi_pg.position)
                    lvi(0).SubItems.Add(pi_pg.salary)
                    lvi(0).SubItems.Add(pi_pg.avg)
                    Try
                        lvi(0).SubItems.Add(scores_by_player(pi_pg.name)(DateTimePicker1.Value.ToShortDateString)(0))
                    Catch ex As Exception
                    End Try

                    lvi(1) = New ListViewItem
                    lvi(1).Text = pi_sg.name
                    lvi(1).SubItems.Add(pi_sg.position)
                    lvi(1).SubItems.Add(pi_sg.salary)
                    lvi(1).SubItems.Add(pi_sg.avg)
                    Try
                        lvi(1).SubItems.Add(scores_by_player(pi_sg.name)(DateTimePicker1.Value.ToShortDateString)(0))
                    Catch ex As Exception
                    End Try

                    lvi(2) = New ListViewItem
                    lvi(2).Text = pi_sf.name
                    lvi(2).SubItems.Add(pi_sf.position)
                    lvi(2).SubItems.Add(pi_sf.salary)
                    lvi(2).SubItems.Add(pi_sf.avg)
                    Try
                        lvi(2).SubItems.Add(scores_by_player(pi_sf.name)(DateTimePicker1.Value.ToShortDateString)(0))
                    Catch ex As Exception
                    End Try

                    lvi(3) = New ListViewItem
                    lvi(3).Text = pi_pf.name
                    lvi(3).SubItems.Add(pi_pf.position)
                    lvi(3).SubItems.Add(pi_pf.salary)
                    lvi(3).SubItems.Add(pi_pf.avg)
                    Try
                        lvi(3).SubItems.Add(scores_by_player(pi_pf.name)(DateTimePicker1.Value.ToShortDateString)(0))
                    Catch ex As Exception
                    End Try

                    lvi(4) = New ListViewItem
                    lvi(4).Text = pi_c.name
                    lvi(4).SubItems.Add(pi_c.position)
                    lvi(4).SubItems.Add(pi_c.salary)
                    lvi(4).SubItems.Add(pi_c.avg)
                    Try
                        lvi(4).SubItems.Add(scores_by_player(pi_c.name)(DateTimePicker1.Value.ToShortDateString)(0))
                    Catch ex As Exception
                    End Try

                    lvi(5) = New ListViewItem
                    lvi(5).Text = pi_g.name
                    lvi(5).SubItems.Add(pi_g.position)
                    lvi(5).SubItems.Add(pi_g.salary)
                    lvi(5).SubItems.Add(pi_g.avg)
                    Try
                        lvi(5).SubItems.Add(scores_by_player(pi_g.name)(DateTimePicker1.Value.ToShortDateString)(0))
                    Catch ex As Exception
                    End Try

                    lvi(6) = New ListViewItem
                    lvi(6).Text = pi_f.name
                    lvi(6).SubItems.Add(pi_f.position)
                    lvi(6).SubItems.Add(pi_f.salary)
                    lvi(6).SubItems.Add(pi_f.avg)
                    Try
                        lvi(6).SubItems.Add(scores_by_player(pi_f.name)(DateTimePicker1.Value.ToShortDateString)(0))
                    Catch ex As Exception
                    End Try

                    lvi(7) = New ListViewItem
                    lvi(7).Text = pi_u.name
                    lvi(7).SubItems.Add(pi_u.position)
                    lvi(7).SubItems.Add(pi_u.salary)
                    lvi(7).SubItems.Add(pi_u.avg)
                    Try
                        lvi(7).SubItems.Add(scores_by_player(pi_u.name)(DateTimePicker1.Value.ToShortDateString)(0))
                    Catch ex As Exception
                    End Try

                    Dim total_salary As Double = pi_pg.salary + pi_sg.salary + pi_sf.salary + pi_pf.salary + pi_c.salary + pi_g.salary + pi_f.salary + pi_u.salary

                    Dim total_points As Double = pi_pg.avg + pi_sg.avg + pi_sf.avg + pi_pf.avg + pi_c.avg + pi_g.avg + pi_f.avg + pi_u.avg

                    Dim total_real As Double = 0
                    For i As Integer = 0 To 7
                        If lvi(i).SubItems.Count > 4 Then
                            Try
                                total_real += CDbl(lvi(i).SubItems(4).Text)
                            Catch ex As Exception
                            End Try
                        End If
                    Next

                    lvi(8) = New ListViewItem
                    lvi(8).Text = "Total:"
                    lvi(8).SubItems.Add("")
                    lvi(8).SubItems.Add(total_salary)
                    lvi(8).SubItems.Add(total_points)
                    lvi(8).BackColor = Color.LightSteelBlue
                    If total_real > 0 Then lvi(8).SubItems.Add(total_real)

                    ListView2.Items.AddRange(lvi)

                    best_score = genetic.Population.Best.Score

                    best_organism = organism.Clone()

                    Application.DoEvents()

                End If


        Dim thisSolution As Double = genetic.Population.Best.Score

        If (Math.Abs(lastSolution - thisSolution) < 1.0) Then
            sameSolutionCount += 1
        Else
            sameSolutionCount = 0
        End If

        lastSolution = thisSolution

        count += 1
            End While

        End If

        Button1.Enabled = True
        Button2.Enabled = True
        Button3.Enabled = True
        Button4.Enabled = True
        'exclude_best = False

    End Sub

    Private Function getGameData() As SortedList

        'GID	Name	        Date	    Team	Opp	H/A	Start	Minutes	GP	obsolete	FDP	S   FP	    obsolete	obsolete	
        '4699	Payton, Elfrid	20150415	orl	    bkn	A	1	    35	    1	24	        20.6	17.75	20.75	    20.75	

        'DDP	DKP	    obsolete	blank	Stats	                            FD Sal	DD Sal	DK Sal	SF Sal	blank	FD pos	
        '21.25	22.75	20.25		        2pt 3rb 6as 4st 1bl 4to 1-5fg 0-2ft	7900	12550	7600	13415		    1	    

        'DD pos	DK pos	SF pos	Team pts	Opp pts
        '1	    1	    G	    88	        101

        Dim otherdata() As String = System.IO.File.ReadAllLines("C:\Users\OK\Desktop\FF\summary.txt")

        Dim games As New SortedList

        Dim missing_players As Integer = 0
        Dim missing_dates As Integer = 0

        For i As Integer = 1 To otherdata.Length - 1
            Dim ds() As String = otherdata(i).Split(":")

            Dim salary As Double = 0
            If ds(21) <> "" And ds(21) <> " " Then salary = CDbl(ds(21).Trim(" "))

            If salary = 0 Then Continue For

            Dim played As Integer = CInt(ds(8))

            If played = 0 Then Continue For

            Dim name As String = ds(1).Split(",")(1).Replace(" ", "") & " " & ds(1).Split(",")(0).Replace(" ", "")

            Dim ai As Integer = averages.IndexOfKey(name)

            If ai = -1 Then Continue For

            Dim dt As Date = Date.Parse(ds(2)(4) & ds(2)(5) & "/" & ds(2)(6) & ds(2)(7) & "/" & ds(2)(0) & ds(2)(1) & ds(2)(2) & ds(2)(3))
            Dim team As String = ds(3).ToUpper
            Dim opp As String = ds(4).ToUpper
            Dim home As Boolean = ds(5).ToUpper = "H"
            Dim dkpoints As Double = CDbl(ds(15))

            Dim ps As player_stats = averages.GetByIndex(ai)

            Dim position As String = ps.position


            Dim info As String = position & "," & name & "," & salary & ","

            If home Then info &= opp & "@" & team & "," Else info &= team & "@" & opp & ","

            info &= ps.fp.ToString("N2") & "," & team


            Dim dtk As Integer = games.IndexOfKey(dt.ToShortDateString)

            If dtk >= 0 Then
                Dim al As ArrayList = games.GetByIndex(dtk)
                If Not al.Contains(info) Then al.Add(info)
                games.SetByIndex(dtk, al)
            Else

                Dim al As New ArrayList

                al.Add(info)

                games.Add(dt.ToShortDateString, al)
            End If

        Next

        Return games
    End Function

    Private Sub findBestModel(ByVal dir As String)

        'applyscores()
        'trainThread(Nothing)

        Dim f() As String = System.IO.Directory.GetFiles(dir, "*.nn")
        Dim scores(f.Length - 1) As Double

        RandomizeArray(f)

        Dim gd As SortedList = getGameData()

        For q As Integer = 0 To f.Length - 1
            loadBinaryData(f(q), model)
            Label1.Text = f(q).Substring(f(q).LastIndexOf("\") + 1)

            Dim count As Integer = 0
            For Each k As String In gd.Keys

                Dim dt As Date = Date.Parse(k)

                ' Set date control to gd
                DateTimePicker1.Value = dt

                ' Set season control to gd
                ComboBox1.SelectedIndex = 1

                ' Populate DK Salaries with gd
                Dim al As ArrayList = gd(k)
                Dim data(al.Count) As String
                data(0) = ""
                For i As Integer = 0 To al.Count - 1
                    data(i + 1) = al(i)
                Next
                loadDataToListView(data, ListView1)

                applyModel()

                trainThread(Nothing)

                For i As Integer = 0 To 7
                    Dim name As String = ListView2.Items(i).SubItems(0).Text
                    Dim si As Integer = scores_by_player.IndexOfKey(name)
                    If si >= 0 Then
                        Dim sl As SortedList = scores_by_player.GetByIndex(si)
                        Dim di As Integer = sl.IndexOfKey(DateTimePicker1.Value.ToShortDateString)
                        If di >= 0 Then scores(q) += CDbl(sl.GetByIndex(di)(0))
                    End If
                Next

                Application.DoEvents()

                count += 1
            Next

            scores(q) /= count

            Try
                System.IO.File.AppendAllText("model_scores.txt", f(q).Substring(f(q).LastIndexOf("\") + 1) & vbTab & scores(q) & vbCrLf)
            Catch ex As Exception
            End Try

        Next

        Array.Sort(scores, f)

        For i As Integer = 0 To f.Length - 1
            f(i) = f(i).Substring(f(i).LastIndexOf("\") + 1) & vbTab & scores(i).ToString("N4")
        Next

        Try
            System.IO.File.WriteAllLines("model_scores.txt", f)
        Catch ex As Exception
        End Try

    End Sub


    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        ListView1.CheckBoxes = True
        ComboBox1.SelectedIndex = 1

        'getAverages3("\FF\raw_data_2012_2013_2014_2015_preseason.txt")
        'saveBinaryData("players.bin", averages)
        'saveBinaryData("scores_by_player.bin", scores_by_player)

        loadBinaryData("players.bin", averages)
        loadBinaryData("scores_by_player.bin", scores_by_player)

        teams.Add("ATL", New Double() {0.172414, 0.43038, 0.955556, 0.479167, 0.0, 0.233766, 0.08, 0.845238, 0.896104, 0.785714, 0.219298})
        teams.Add("BKN", New Double() {0.586207, 0.278481, 0.222222, 0.375, 0.324675, 0.272727, 0.22, 0.416667, 0.454545, 0.529762, 0.596491})
        teams.Add("BOS", New Double() {0.655172, 0.708861, 0.6, 0.270833, 0.428571, 0.441558, 0.34, 0.392857, 0.376623, 0.517857, 0.342105})
        teams.Add("CHA", New Double() {0.931034, 0.316456, 0.111111, 0.0, 0.090909, 1.0, 0.44, 0.0, 0.051948, 0.27381, 0.245614})
        teams.Add("CHI", New Double() {0.310345, 0.329114, 0.311111, 0.354167, 0.727273, 0.363636, 0.7, 0.392857, 0.545455, 0.696429, 0.289474})
        teams.Add("CLE", New Double() {0.103448, 0.253165, 0.4, 0.458333, 0.701299, 0.402597, 0.66, 0.761905, 0.818182, 0.875, 0.517544})
        teams.Add("DAL", New Double() {0.137931, 0.582278, 0.4, 0.145833, 0.285714, 0.077922, 0.0, 0.702381, 0.727273, 0.845238, 0.482456})
        teams.Add("DEN", New Double() {0.689655, 0.746835, 0.2, 0.3125, 0.675325, 0.402597, 0.4, 0.27381, 0.311688, 0.511905, 0.640351})
        teams.Add("DET", New Double() {0.551724, 0.303797, 0.244444, 0.229167, 0.818182, 0.441558, 0.52, 0.309524, 0.272727, 0.553571, 0.526316})
        teams.Add("GSW", New Double() {0.034483, 1.0, 1.0, 0.395833, 0.350649, 0.376623, 0.46, 1.0, 1.0, 0.994048, 0.0})
        teams.Add("HOU", New Double() {0.37931, 0.822785, 0.266667, 0.791667, 0.701299, 0.181818, 0.42, 0.666667, 0.701299, 0.666667, 0.201754})
        teams.Add("IND", New Double() {0.793103, 0.341772, 0.288889, 0.395833, 0.337662, 0.818182, 0.68, 0.333333, 0.376623, 0.464286, 0.236842})
        teams.Add("LAC", New Double() {0.0, 0.531646, 0.755556, 0.083333, 0.181818, 0.532468, 0.4, 0.916667, 0.922078, 1.0, 0.421053})
        teams.Add("LAL", New Double() {0.758621, 0.443038, 0.155556, 0.1875, 0.519481, 0.61039, 0.4, 0.202381, 0.246753, 0.464286, 0.859649})
        teams.Add("MEM", New Double() {0.413793, 0.177215, 0.355556, 0.291667, 0.428571, 0.480519, 0.46, 0.392857, 0.480519, 0.60119, 0.149123})
        teams.Add("MIA", New Double() {0.724138, 0.063291, 0.177778, 0.6875, 0.233766, 0.233766, 0.2, 0.52381, 0.597403, 0.505952, 0.491228})
        teams.Add("MIL", New Double() {0.827586, 0.468354, 0.555556, 0.895833, 0.519481, 0.220779, 0.34, 0.511905, 0.545455, 0.446429, 0.096491})
        teams.Add("MIN", New Double() {0.862069, 0.531646, 0.244444, 0.520833, 0.675325, 0.0, 0.12, 0.142857, 0.298701, 0.404762, 1.0})
        teams.Add("NO", New Double() {0.275862, 0.113924, 0.4, 0.270833, 0.74026, 0.454545, 0.66, 0.535714, 0.558442, 0.738095, 0.570175})
        teams.Add("NYK", New Double() {0.965517, 0.113924, 0.311111, 0.583333, 0.402597, 0.194805, 0.06, 0.166667, 0.181818, 0.244048, 0.789474})
        teams.Add("OKC", New Double() {0.344828, 0.696203, 0.0, 0.395833, 0.974026, 0.519481, 0.96, 0.428571, 0.493506, 0.684524, 0.429825})
        teams.Add("ORL", New Double() {0.896552, 0.392405, 0.2, 0.583333, 0.220779, 0.571429, 0.24, 0.452381, 0.38961, 0.392857, 0.614035})
        teams.Add("PHI", New Double() {1.0, 0.696203, 0.044444, 1.0, 0.532468, 0.181818, 0.06, 0.035714, 0.0, 0.0, 0.342105})
        teams.Add("PHX", New Double() {0.448276, 0.746835, 0.022222, 0.520833, 0.402597, 0.246753, 0.2, 0.535714, 0.558442, 0.589286, 0.45614})
        teams.Add("POR", New Double() {0.241379, 0.468354, 0.333333, 0.291667, 0.363636, 0.597403, 0.58, 0.619048, 0.649351, 0.744048, 0.280702})
        teams.Add("SAC", New Double() {0.517241, 0.64557, 0.044444, 0.75, 0.662338, 0.467532, 0.8, 0.404762, 0.623377, 0.565476, 0.72807})
        teams.Add("SAS", New Double() {0.206897, 0.392405, 0.711111, 0.395833, 0.25974, 0.74026, 0.62, 0.72619, 0.792208, 0.785714, 0.122807})
        teams.Add("TOR", New Double() {0.068966, 0.329114, 0.177778, 0.166667, 0.545455, 0.220779, 0.34, 0.619048, 0.766234, 0.89881, 0.578947})
        teams.Add("UTA", New Double() {0.482759, 0.0, 0.133333, 0.708333, 1.0, 0.61039, 1.0, 0.452381, 0.480519, 0.565476, 0.342105})
        teams.Add("WAS", New Double() {0.62069, 0.405063, 0.644444, 0.583333, 0.454545, 0.74026, 0.78, 0.511905, 0.519481, 0.52381, 0.157895})

    End Sub

    Private Sub ListView1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListView1.SelectedIndexChanged

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        OpenFileDialog1.ShowDialog()
        If System.IO.File.Exists(OpenFileDialog1.FileName) Then
            ListView1.Items.Clear()
            Dim data() As String = System.IO.File.ReadAllLines(OpenFileDialog1.FileName)
            loadDataToListView(data, ListView1)
        End If
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        OpenFileDialog2.ShowDialog()
        If System.IO.File.Exists(OpenFileDialog2.FileName) Then
            Try
                loadBinaryData(OpenFileDialog2.FileName, model2)
                model = Nothing
            Catch ex As Exception
                Try
                    loadBinaryData(OpenFileDialog2.FileName, model)
                    model2 = Nothing
                Catch ex2 As Exception
                End Try
            End Try
            Label1.Text = OpenFileDialog2.FileName.Substring(OpenFileDialog2.FileName.LastIndexOf("\") + 1)
        End If
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click

        If model Is Nothing And model2 Is Nothing Then
            MsgBox("No model loaded!", MsgBoxStyle.Exclamation)
            Exit Sub
        End If

        If ListView1.Items.Count > 0 Then
            Button3.Enabled = False
            applyModel()
            Button3.Enabled = True
        Else
            MsgBox("No salaries loaded!", MsgBoxStyle.Exclamation)
            Exit Sub
        End If

    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        Button1.Enabled = False
        Button2.Enabled = False
        Button3.Enabled = False
        Button4.Enabled = False
        trainThread(Nothing)
    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click

        'If ListView1.Items.Count > 0 Then
        Button6.Enabled = False

        FolderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyDocuments
        FolderBrowserDialog1.ShowDialog()

        If System.IO.Directory.Exists(FolderBrowserDialog1.SelectedPath) Then
            findBestModel(FolderBrowserDialog1.SelectedPath)
        End If

        Button6.Enabled = True
        'Else
        '    MsgBox("No salaries loaded!", MsgBoxStyle.Exclamation)
        '    Exit Sub
        'End If

    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click

        If ListView1.Items.Count = 0 Then Exit Sub

        Dim data As String = ""
        Try
            Dim request As System.Net.HttpWebRequest = _
                System.Net.HttpWebRequest.Create("http://www.rotoworld.com/teams/injuries/nba/all/")
            Dim response As System.Net.HttpWebResponse = request.GetResponse()
            Dim sr As System.IO.StreamReader = New System.IO.StreamReader(response.GetResponseStream())
            data = sr.ReadToEnd()
            sr.Close()
            sr.Dispose()
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

        data = data.ToLower()

        For i As Integer = 0 To ListView1.Items.Count - 1
            If Not ListView1.Items(i).Checked Then Continue For
            If data.IndexOf(ListView1.Items(i).SubItems(0).Text.ToLower) >= 0 Then
                ListView1.Items(i).Checked = False
            End If
        Next

    End Sub

End Class
