using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using System.Collections.Generic;
using System.Linq;
using System;
using System.CodeDom;


namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public sealed class MyStrategy : IStrategy
    {

        //        public static VisualClient vc;

        public static World worldConst;
        public static Player meConst;
        public static Player ownConst;
        public static Game gameConst;

        public static List<MyVencicle> allObject;
        public static myCell[,] myMap;
        public static List<Move> pool;
        public static List<myFormation> formation;

        public static List<Facility> netralFac;

        public static string step;
        public static int globalWait;
        public static List<myFactilies> myFacList;




        public class myFactilies
        {
            public Facility f;
            public double xCenter, yCenter;
            public bool isMe;
            public bool isNeutral;
            public bool isOwn;
            public FacilityType ft;
            public bool IsMyTerritory;
            public double distTo;

            public myFactilies(Facility fac)
            {
                f = fac;
                xCenter = f.Left + 32;
                yCenter = f.Top + 32;
                isMe = false;
                if (fac.OwnerPlayerId == meConst.Id)
                    isMe = true;
                isNeutral = false;
                if (fac.OwnerPlayerId < 0)
                    isNeutral = true;
                isOwn = false;
                if (fac.OwnerPlayerId > 0 && !isMe)
                    isOwn = true;

                ft = fac.Type;
            }
            public void update()
            {
                f = worldConst.Facilities.First(i => i.Id == f.Id);
                if (f == null)
                    return;
                isMe = false;
                if (f.OwnerPlayerId == meConst.Id)
                    isMe = true;
                isNeutral = false;
                if (f.OwnerPlayerId < 0)
                    isNeutral = true;
                isOwn = false;
                if (f.OwnerPlayerId > 0 && !isMe)
                    isOwn = true;
            }
        }

        public class myFormation
        {
            public int lastTick;
            public int numGroup;
            public List<MyVencicle> vencicleFormation;
            public List<Move> poolMove;
            public bool isArial;
            public double xCenter, yCenter;
            public int mapX, mapY;
            public bool waitMove;
            public VehicleType venType;
            public int waitToNextCmd;
            public int waitGetFac;
            public List<myCell> cellList;

            public int targetX, targetY;

            public myFactilies myFac;
            public bool isNuclearScale;
            public double scaleNuclearX, scaleNuclearY;
            public bool isScale;
            public bool isRotate;
            public bool isDeath;
            public int prevCount;
            public bool isNuclear;
            public double tarX, tarY;
            public bool isStop;
            public bool waitToCapture;
            public bool isNeedRescale;
            public int CaptureStep;

            public myCell razvedCell;

            public bool isWaitFullMove;

            public myFormation()
            {
                vencicleFormation = new List<MyVencicle>();
                poolMove = new List<Model.Move>();
                numGroup = 0;
                isArial = false;
                targetX = 0;
                targetY = 0;
                waitToNextCmd = 0;
                waitGetFac = 0;
                myFac = null;
                isNuclearScale = false;
                isScale = false;
                isRotate = false;
                isDeath = false;
                prevCount = 0;
                isNuclear = false;
                tarX = 0;
                tarY = 0;
                isStop = true;
                myFac = null;
                waitToCapture = false;
                razvedCell = null;
                CaptureStep = 0;
                lastTick = worldConst.TickIndex;
                isWaitFullMove = false;
            }

            public void update()
            {
                if (vencicleFormation.Count == 0)
                    return;
                if (vencicleFormation.Where(i => i.v.IsAerial).Count() > 0)
                    isArial = true;
                else
                    isArial = false;
                xCenter = vencicleFormation.Average(i => i.v.X);
                yCenter = vencicleFormation.Average(i => i.v.Y);
                mapX = (int)Math.Floor(xCenter / 32);
                mapY = (int)Math.Floor(yCenter / 32);
                venType = GetVehicleType();
                isDeath = false;
                if (prevCount != vencicleFormation.Count)
                {
                    if (prevCount > 0)
                        isDeath = true;
                    prevCount = vencicleFormation.Count;
                }
                isStop = false;

            }
            public void Main()
            {

                if (vencicleFormation.Count() == 0)
                {
                    myFac = null;
                    return;
                }

                Player own = worldConst.Players.First(i => !i.IsMe);
                if (own.NextNuclearStrikeTickIndex > 0 && isNuclearScale == false)
                {
                    scaleNuclearX = own.NextNuclearStrikeX;
                    scaleNuclearY = own.NextNuclearStrikeY;
                    if (isMeNuclear(scaleNuclearX, scaleNuclearY))
                    {
                        isNuclearScale = true;
                        pool = new List<Model.Move>();
                        Scale(3, scaleNuclearX, scaleNuclearY);
                        return;
                    }
                }

                if (isNuclearScale)
                {
                    if (own.NextNuclearStrikeTickIndex < 0)
                    {
                        Scale(0.1);
                        waitMove = true;
                        waitToNextCmd = 10;
                        isNuclearScale = false;
                        return;
                    }
                    return;
                }


                if (own.NextNuclearStrikeTickIndex > 0)
                    return;

                if (waitToNextCmd > 0)
                {
                    waitToNextCmd--;
                    return;
                }


                if (isNuclear)
                {
                    Nuclear();
                    return;
                }

                if (venType == VehicleType.Fighter && isNuclear == false)
                {
                    Fighter();
                    return;
                }
                if (venType == VehicleType.Helicopter)
                {
                    Helicopter();
                    return;
                }
                if (!isArial)
                    Ground();
            }


            public bool isMeNuclear(double nucX, double nucY)
            {
                foreach (MyVencicle mv in vencicleFormation)
                {
                    double dst = getDistanceSQR(mv.v.X, mv.v.Y, nucX, nucY);
                    if (dst < 50 * 50)
                        return true;
                }
                return false;
            }

            public void Scale(double koef)
            {
                isWaitFullMove = true;
                lastTick = worldConst.TickIndex;
                Move m = new Model.Move();
                m.Action = ActionType.Scale;
                m.X = xCenter;
                m.Y = yCenter;
                m.Factor = koef;
                poolMove.Add(m);
                return;
            }

            public void Roatate(double an)
            {
                isWaitFullMove = true;
                lastTick = worldConst.TickIndex;
                Move m = new Model.Move();
                m.Action = ActionType.Rotate;
                m.X = xCenter;
                m.Y = yCenter;
                m.Angle = an;
                poolMove.Add(m);
                return;
            }

            public void Scale(double koef, double xx, double yy)
            {
                isWaitFullMove = true;
                Move m = new Model.Move();
                m.Action = ActionType.Scale;
                m.X = xx;
                m.Y = yy;
                m.Factor = koef;
                poolMove.Add(m);
                waitMove = true;
                isNeedRescale = false;
                if (koef < 1)
                    isNeedRescale = true;

                return;
            }


            public myFormation checkCollis()
            {
                foreach (myFormation f in formation.Where(i =>
                    i.vencicleFormation.Count > 0 && i.isArial == isArial && i.numGroup != numGroup))
                {
                    foreach (MyVencicle mv in vencicleFormation)
                        foreach (MyVencicle obj in f.vencicleFormation)
                        {
                            if (getDistanceSQR(mv, obj) < 32 * 32)
                                return f;
                        }
                }
                return null;
            }


            public void Nuclear()
            {
                if (waitMove && isMove())
                    return;

                List<myCell> mapList = new List<myCell>();
                if (vencicleFormation.Count == 0)
                    return;
                MyVencicle nuc = vencicleFormation[0];
                myCell bestCell = null;
                bool isHealth;

                if (meConst.RemainingNuclearStrikeCooldownTicks > 300)
                {
                    if (nuc.v.Durability < 99)
                    {
                        myFormation fArv = formation.First(i => i.venType == VehicleType.Arrv);
                        if (fArv.vencicleFormation.Count > 0)
                        {
                            bestCell = myMap[fArv.mapX, fArv.mapY];
                        }
                    }
                }

                MyVencicle enemyArial = null;
                double dstToEnemy = 0;
                foreach (MyVencicle enemy in allObject.Where(i => i.isMe == false && (i.v.IsAerial || i.v.Type == VehicleType.Ifv)))
                {
                    double dst = getDistanceSQR(enemy, nuc);
                    if (dstToEnemy == 0 || dstToEnemy > dst)
                    {
                        dstToEnemy = dst;
                        enemyArial = enemy;
                    }
                }
                //dstToEnemy = Math.Sqrt(dstToEnemy);

                foreach (myCell mc in myMap)
                {
                    mapList.Add(mc);
                }


                if (bestCell == null)
                {
                    foreach (myCell mc in myMap)
                    {
                        if (bestCell == null)
                            bestCell = mc;
                        if (bestCell.cntEnemyTank + bestCell.cntEnemyIvf < mc.cntEnemyTank + mc.cntEnemyIvf)
                        {
                            bestCell = mc;
                        }
                    }
                    if (bestCell.cntEnemyTank + bestCell.cntEnemyIvf < 5)
                    {
                        bestCell = null;
                        double bestDst = 0;
                        foreach (Facility f in worldConst.Facilities.Where(i => i.OwnerPlayerId > 0 && i.OwnerPlayerId != meConst.Id))
                        {
                            double dst = getDistanceSQR(nuc.v.X, nuc.v.Y, f.Left, f.Top);
                            if (bestDst < dst)
                            {
                                bestDst = dst;
                                bestCell = new myCell(f.Left + 64, f.Top + 64);
                            }
                        }

                        if (bestCell != null)
                            razvedCell = null;

                        if (razvedCell != null)
                            if (getDistanceSQR(nuc.v.X, nuc.v.Y, razvedCell.x, razvedCell.y) < 80 * 80)
                                razvedCell = null;

                        if (razvedCell != null)
                            bestCell = razvedCell;
                        if (bestCell == null)
                        {
                            foreach (Facility f in worldConst.Facilities.Where(i => i.OwnerPlayerId != meConst.Id))
                            {
                                double dst = getDistanceSQR(nuc.v.X, nuc.v.Y, f.Left, f.Top);
                                if (bestDst < dst)
                                {
                                    bestDst = dst;
                                    bestCell = new myCell(f.Left + 64, f.Top + 64);
                                    razvedCell = bestCell;
                                }
                            }
                        }
                    }
                }
                if (bestCell == null)
                {
                    bestCell = new myCell((new Random().NextDouble()) * 1020, (new Random().NextDouble()) * 1020);
                    razvedCell = bestCell;
                }
                double point = 0;
                double bestPoint = 0;
                double bestX = 0, bestY = 0;
                for (int x = -4; x < 5; x++)
                    for (int y = -4; y < 5; y++)
                    {
                        nuc.myX = nuc.v.X + x * 8;
                        nuc.myY = nuc.v.Y + y * 8;
                        if (nuc.myX < 10 || nuc.myX > 1020 || nuc.myY < 10 || nuc.myY > 1020)
                            continue;
                        double dstToBest = 0;
                        point = 0;
                        if (dstToEnemy > 0 && dstToEnemy < 160 * 160)
                        {
                            double newdstToEnemy = (getDistanceSQR(nuc.myX, nuc.myY, enemyArial.v.X, enemyArial.v.Y));
                            if (newdstToEnemy > dstToEnemy)
                                point = point + newdstToEnemy;
                        }
                        if (bestCell != null)
                        {
                            dstToBest = getDistanceSQR(nuc.myX, nuc.myY, bestCell.x, bestCell.y);
                            point = point + 1000 / dstToBest;
                        }

                        foreach (MyVencicle mv in allObject.Where(i => i.v.Type != VehicleType.Arrv))
                        {
                            if (mv.v.Id == nuc.v.Id)
                                continue;
                            double dst = getDistanceSQR(nuc.myX, nuc.myY, mv.v.X, mv.v.Y);
                            if (mv.isMe == false && mv.v.IsAerial == false && dst < nuc.visionRange * nuc.visionRange && dst > 50 * 50)
                                point = point + 2;
                            if (mv.isMe == false && mv.v.IsAerial == false && dst < nuc.visionRange * nuc.visionRange && dst < 30 * 30)
                                point = point - 2;
                            if (mv.isMe == true && mv.v.IsAerial == true && dst < 20 * 20)
                            {
                                point = -1 / dst;
                                break;
                            }
                        }
                        if (bestPoint == 0 || bestPoint < point)
                        {
                            bestPoint = point;
                            bestX = nuc.myX;
                            bestY = nuc.myY;
                        }
                    }
                if (bestX > 0)
                {
                    MoveToXY(bestX, bestY);
                    waitMove = true;
                }
            }

            public double dstInv(double dst)
            {
                return (2 * 1024 * 1024 - dst) / (2 * 1024 * 1024);
            }


            public bool isCollision()
            {
                bool isArrailThis = false;
                if (vencicleFormation[0].v.IsAerial)
                    isArrailThis = true;
                List<MyVencicle> collisionList = new List<MyVencicle>();
                foreach (MyVencicle mv in allObject.Where(i => i.isMe && i.v.IsAerial == isArrailThis))
                {
                    if (mv.groupNum == numGroup)
                        continue;
                    foreach (MyVencicle myV in vencicleFormation)
                    {
                        double dstTome = getDistanceSQR(mv, myV);
                        if (dstTome < 32 * 32)
                        {
                            if (collisionList.Where(i => i.v.Id == mv.v.Id).Count() == 0)
                                collisionList.Add(mv);
                        }
                    }
                }
                if (collisionList.Count == 0)
                    return false;



                return true;
            }

            public void Ground()
            {

                if (waitMove && isMove())
                    return;
                double minDst = 0;
                double minNear = 0;

                if (myFac != null)
                    if (myFac.isMe && myFac.f.CapturePoints == 100)
                    {
                        myFac = null;
                        waitToCapture = false;
                    }

                if (waitToCapture && myFac != null)
                {

                    int cntTank = 0, cntHel = 0, cntIfv = 0, cntAll = 0;
                    foreach (MyVencicle obj in allObject.Where(i => !i.isMe && i.v.Type != VehicleType.Fighter && i.v.Type != VehicleType.Arrv))
                    {
                        double dst = getDistanceSQR(xCenter, yCenter, obj.v.X, obj.v.Y);
                        if (dst < 80 * 80)
                        {
                            if (obj.v.Type == VehicleType.Tank)
                                cntTank++;
                            if (obj.v.Type == VehicleType.Helicopter)
                                cntHel++;
                            if (obj.v.Type == VehicleType.Ifv)
                                cntIfv++;
                            cntAll++;
                        }
                    }
                    if (venType == VehicleType.Arrv && cntAll > 10)
                    {
                        waitToCapture = false;
                        return;
                    }
                    if (venType == VehicleType.Ifv && cntTank > 15)
                    {
                        waitToCapture = false;
                        CaptureStep = 999;
                        myFac = null;
                        return;
                    }


                    if (CaptureStep == 1)
                    {
                        Scale(0.1);
                        waitMove = true;
                        CaptureStep = 2;
                        return;
                    }
                    if (CaptureStep == 2)
                    {
                        Roatate(Math.PI / 2);
                        waitMove = true;
                        CaptureStep = 3;
                        return;
                    }
                    if (CaptureStep == 3)
                    {
                        Scale(0.1);
                        waitMove = true;
                        CaptureStep = 4;
                        return;
                    }
                    return;
                }
                waitToCapture = false;

                myFac = null;

                if (CaptureStep != 999)
                {

                    int cntFacEnemy = myFacList.Where(i => i.isMe == false || i.f.CapturePoints < 99).Count();
                    if (cntFacEnemy > 0)
                    {
                        List<myFactilies> nearFac = new List<myFactilies>();
                        foreach (myFactilies ff in myFacList)
                        {
                            double dst = getDistanceSQR(xCenter, yCenter, ff.xCenter, ff.yCenter);
                            ff.distTo = dst;
                            nearFac.Add(ff);
                        }
                        if (nearFac.Count > 0)
                            if (nearFac.Count(i => i.isMe == false) > 0)
                            {
                                myFactilies fNearest = nearFac.OrderBy(i => i.distTo).First(i => i.isMe == false);
                                if (fNearest.distTo < 55 * 55 && checkCollis() == null)
                                {
                                    if (venType == VehicleType.Arrv && fNearest.distTo > 20 * 20 && fNearest.isOwn == true)
                                    {
                                        MoveToXY(fNearest.xCenter, fNearest.yCenter - 30);
                                    }
                                    else
                                    {
                                        if (fNearest.distTo > 10 * 10)
                                            MoveToXY(fNearest.xCenter, fNearest.yCenter);
                                    }

                                    waitToCapture = true;
                                    CaptureStep = 1;
                                    myFac = fNearest;
                                    waitMove = true;
                                    return;
                                }
                            }

                        if (vencicleFormation.Count < 50)
                        {
                            foreach (myFactilies nFac in nearFac.Where(i => i.isMe == false || (i.isMe == true && i.f.CapturePoints < 99)).OrderBy(i => i.distTo))
                            {
                                bool isUse = false;
                                int cnt = 0;
                                foreach (myFormation mf in formation.Where(i => i.myFac != null))
                                {
                                    if (mf.numGroup == numGroup || mf.vencicleFormation.Count == 0)
                                        continue;
                                    if (mf.myFac.f.Id == nFac.f.Id)
                                    {
                                        cnt = cnt + mf.vencicleFormation.Count;
                                        isUse = true;
                                    }
                                }

                                if (isUse == false || (nFac.isOwn && cnt < 80))
                                {
                                    myFac = nFac;
                                    break;
                                }
                            }
                        }

                        if (myFac == null)
                        {
                            foreach (myFactilies nFac in nearFac.Where(i => i.f.Type == FacilityType.VehicleFactory && (i.isMe == false || (i.isMe == true && i.f.CapturePoints < 99))).OrderBy(i => i.distTo))
                            {
                                bool isUse = false;
                                int cnt = 0;
                                double myDstToFac = getDistanceSQR(xCenter, yCenter, nFac.xCenter, nFac.yCenter);
                                foreach (myFormation mf in formation.Where(i => i.myFac != null))
                                {
                                    if (mf.numGroup == numGroup || mf.vencicleFormation.Count == 0)
                                        continue;
                                    if (mf.myFac.f.Id == nFac.f.Id)
                                    {
                                        if (myDstToFac < getDistanceSQR(mf.xCenter, mf.yCenter, nFac.xCenter, nFac.yCenter))
                                        {
                                            mf.myFac = null;
                                            continue;
                                        }
                                        if (nFac.isOwn == false && mf.venType != VehicleType.Arrv)
                                        {
                                            cnt = cnt + mf.vencicleFormation.Count;
                                            isUse = true;
                                        }
                                    }
                                }

                                if (isUse == false || (cnt < 60 && nFac.isMe == false))
                                {
                                    myFac = nFac;
                                    break;
                                }
                            }
                        }

                        if (myFac == null)
                        {
                            foreach (myFactilies nFac in nearFac.Where(i => i.f.Type == FacilityType.ControlCenter && (i.isMe == false || (i.isMe == true && i.f.CapturePoints < 99))).OrderBy(i => i.distTo))
                            {
                                bool isUse = false;
                                int cnt = 0;
                                foreach (myFormation mf in formation.Where(i => i.myFac != null))
                                {
                                    if (mf.numGroup == numGroup)
                                        continue;
                                    if (mf.myFac.f.Id == nFac.f.Id)
                                    {
                                        cnt = cnt + mf.vencicleFormation.Count;
                                        isUse = true;
                                    }
                                }

                                if (isUse == false || (cnt < 20 && nFac.isMe == false))
                                {
                                    myFac = nFac;
                                    break;
                                }
                            }
                        }
                    }

                }

                isScale = false;
                isRotate = false;
                if (venType == VehicleType.Arrv)
                {
                    Arrv();
                    return;
                }


                double bestX = -1, bestY = -1;
                double bestPoint = 0;
                for (int x = -1; x < 2; x++)
                    for (int y = -1; y < 2; y++)
                    {
                        bool isBreak = false;
                        double point = 0;
                        foreach (MyVencicle mvForm in vencicleFormation)
                        {
                            MyVencicle mv = mvForm;
                            mv.myX = mv.v.X + x * 32;
                            mv.myY = mv.v.Y + y * 32;
                            foreach (MyVencicle obj in allObject)
                            {
                                if (mv.myX < 2 || mv.myX > 1020 || mv.myY < 2 || mv.myY > 1020)
                                {
                                    point = -100000;
                                    isBreak = true;
                                    break;
                                }

                                if (obj.isMe && obj.v.IsAerial)
                                    continue;
                                if (obj.isMe && obj.groupNum == numGroup)
                                    continue;
                                double dst = getDistanceSQR(mv.myX, mv.myY, obj.v.X, obj.v.Y);
                                if (obj.isMe && !obj.v.IsAerial && dst < 25 * 25)
                                {
                                    if (obj.v.Type != mv.v.Type || obj.groupNum == 0)
                                    {
                                        if (obj.v.Type != VehicleType.Tank || dst < 10 * 10)
                                        {
                                            point = -(2 * 1024 * 1024 - getDistanceSQR(xCenter + 32 * x,
                                                          yCenter + 32 * y, obj.v.X, obj.v.Y));
                                            isBreak = true;
                                            break;
                                        }
                                    }
                                }

                                if (!obj.isMe && dst < 30 * 30)
                                {
                                    if (mv.v.Type == VehicleType.Ifv)
                                    {
                                        if (obj.v.Type == VehicleType.Fighter && myFac == null)
                                            point = point + 20;
                                        if (obj.v.Type == VehicleType.Helicopter && myFac == null)
                                            point = point + 10;
                                        if (obj.v.Type == VehicleType.Ifv && myFac == null)
                                            point = point + 1;
                                        if (obj.v.Type == VehicleType.Tank)
                                            point = point - 20;
                                        if (obj.v.Type == VehicleType.Arrv)
                                            point = point + 30;
                                    }
                                    if (mv.v.Type == VehicleType.Tank)
                                    {
                                        if (obj.v.Type == VehicleType.Helicopter)
                                            point = point - 20;
                                        if (obj.v.Type == VehicleType.Ifv)
                                            point = point + 20;
                                        if (obj.v.Type == VehicleType.Tank)
                                            point = point + 1;
                                        if (obj.v.Type == VehicleType.Arrv)
                                            point = point + 30;
                                    }
                                }

                            }
                            if (isBreak)
                                break;
                        }
                        if (point == 0)
                            CaptureStep = 0;
                        if (point == 0 && myFac != null)
                        {
                            double bestDstFax = getDistanceSQR(xCenter + x * 32, yCenter + y * 32, myFac.xCenter, myFac.yCenter);
                            point = (2 * 1024 * 1024 - bestDstFax) / (2 * 1024 * 1024);
                        }

                        double bestDst = 0;
                        if (point == 0)
                        {
                            for (int mx = 0; mx < 32; mx++)
                                for (int my = 0; my < 32; my++)
                                {
                                    if (myMap[mx, my].cntEnemyAll > 0)
                                    {
                                        double dst = getDistanceSQR(myMap[mx, my].x, myMap[mx, my].y, xCenter + x * 32, yCenter + y * 32);
                                        if (dst < bestDst || bestDst == 0)
                                        {
                                            bestDst = dst;
                                            point = (2 * 1024 * 1024 - dst) / (2 * 1024 * 1024);
                                        }
                                    }

                                }
                        }
                        if (point > bestPoint || bestPoint == 0 && (x != 0 || y != 0))
                        {
                            bestPoint = point;
                            bestX = xCenter + x * 32;
                            bestY = yCenter + y * 32;
                        }
                    }
                if (bestX > 0)
                {
                    MoveToXY(bestX, bestY);
                    waitMove = true;
                }
                if (bestX <= 0)
                {

                }

            }

            public void Arrv()
            {


                double bestX = -1, bestY = -1;
                double bestPoint = 0;

                MyVencicle nearEnemy = null;

                foreach (MyVencicle obj in allObject.Where(i => i.isMe == false && i.v.Type != VehicleType.Fighter && i.v.Type != VehicleType.Arrv))
                {
                    double dst = getDistanceSQR(xCenter, yCenter, obj.v.X, obj.v.Y);
                    if (dst < 32 * 32 * 8 && obj.v.Type == VehicleType.Helicopter)
                    {
                        nearEnemy = obj;
                        break;
                    }
                    if (dst < 32 * 32 * 3)
                        foreach (MyVencicle mv in vencicleFormation)
                        {
                            double dst2 = getDistanceSQR(mv.v.X, mv.v.Y, obj.v.X, obj.v.Y);
                            if (dst2 < 40 * 40)
                            {
                                nearEnemy = obj;
                                break;
                            }
                        }
                }

                if (nearEnemy != null)
                {
                    double bestDst = getDistanceSQR(xCenter, yCenter, nearEnemy.v.X, nearEnemy.v.Y);
                    for (int x = -1; x < 2; x++)
                        for (int y = -1; y < 2; y++)
                        {
                            double dst = getDistanceSQR(xCenter + 16 * x, yCenter + 16 * y, nearEnemy.v.X, nearEnemy.v.Y);
                            if (dst > bestDst)
                            {
                                bestDst = dst;
                                bestX = xCenter + 16 * x;
                                bestY = yCenter + 16 * y;
                            }
                        }

                }

                if (bestX > 0)
                {
                    MoveToXY(bestX, bestY);
                    return;
                }


                for (int x = -2; x < 3; x++)
                    for (int y = -2; y < 3; y++)
                    {
                        bool isBreak = false;
                        double point = 0;
                        foreach (MyVencicle mvForm in vencicleFormation)
                        {
                            MyVencicle mv = mvForm;
                            mv.myX = mv.v.X + x * 32;
                            mv.myY = mv.v.Y + y * 32;
                            foreach (MyVencicle obj in allObject)
                            {
                                if (mv.myX < 1 || mv.myX > 1020 || mv.myY < 1 || mv.myY > 1020)
                                {
                                    point = -100000;
                                    isBreak = true;
                                    break;
                                }

                                if (obj.isMe && obj.v.IsAerial)
                                    continue;
                                if (obj.isMe && obj.groupNum == numGroup)
                                    continue;
                                double dst = getDistanceSQR(mv.myX, mv.myY, obj.v.X, obj.v.Y);
                                if (obj.isMe && !obj.v.IsAerial && dst < 45 * 45)
                                {
                                    point = point - 2 * 1024 / dst;
                                    break;
                                }

                                if (!obj.isMe && obj.v.Type != VehicleType.Fighter && obj.v.Type != VehicleType.Arrv && dst < 32 * 32 * 4 * 4)
                                {
                                    point = point - 2 * 1024 / dst;
                                    break;
                                }
                                if (!obj.isMe && obj.v.Type == VehicleType.Arrv && dst < 32 * 32)
                                {
                                    point = point - 2 * 1024 / dst;
                                    break;
                                }

                            }
                            if (isBreak)
                                break;
                        }
                        if (isBreak)
                            continue;

                        if (point == 0 && myFac != null)
                        {
                            double bestDstFax = getDistanceSQR(xCenter + x * 32, yCenter + y * 32, myFac.xCenter, myFac.yCenter);
                            point = (2 * 1024 * 1024 - bestDstFax) / (2 * 1024 * 1024);
                        }

                        double bestDst = 0;
                        if (point == 0)
                        {
                            point = getDistanceSQR(xCenter + x * 32, yCenter + y * 32, 1020, 1020);
                        }
                        if (point > bestPoint || bestPoint == 0)
                        {
                            bestPoint = point;
                            bestX = xCenter + x * 32;
                            bestY = yCenter + y * 32;
                        }
                    }
                if (bestX > 0)
                {
                    MoveToXY(bestX, bestY);
                    waitMove = true;
                }

            }

            public void Fighter_2()
            {
                double bestDmgX = 0, bestDmgY = 0;
                int[,] setka = new int[1024, 1024];
                int bestX = -1, bestY = -1;
                int best = 0;

                MyVencicle nearEnemy = null;



                double nearDst = 0;
                foreach (MyVencicle en in allObject.Where(i => !i.isMe && i.v.IsAerial))
                {
                    double dst = getDistanceSQR(xCenter, yCenter, en.v.X, en.v.Y);
                    if (nearDst == 0 || dst < nearDst)
                    {
                        nearDst = dst;
                        nearEnemy = en;
                    }
                }

                if (nearEnemy == null)
                    return;

                MyVencicle myNear = null;
                double bestDst = 0;
                foreach (MyVencicle mv in vencicleFormation)
                {
                    double dst = getDistanceSQR(mv, nearEnemy);
                    if (bestDst == 0 || bestDst > dst)
                    {
                        bestDst = dst;
                        myNear = mv;
                    }
                }

                if (myNear == null)
                    return;


                double moveX = xCenter, moveY = yCenter;


                double point = 0;
                double Bestpoint = 0;
                int cntReman = 0;
                int temp = 0;
                int MaxReman = 0;

                for (int x = -2; x < 3; x++)
                    for (int y = -2; y < 3; y++)
                    {
                        cntReman = 0;
                        point = 1 / getDistanceSQR(xCenter + 8 * x, yCenter + 8 * y, nearEnemy.v.X, nearEnemy.v.Y);

                        foreach (MyVencicle mv in vencicleFormation)
                        {
                            mv.myX = mv.v.X + 8 * x;
                            mv.myY = mv.v.Y + 8 * y;
                            temp = 0;
                            foreach (MyVencicle enemy in allObject.Where(i => !i.isMe && (i.v.IsAerial || i.v.Type == VehicleType.Ifv)))
                            {

                                double dst = getDistanceSQR(mv.myX, mv.myY, enemy.v.X, enemy.v.Y);

                                if (enemy.v.IsAerial)
                                {
                                    if (mv.v.SquaredAerialAttackRange > dst && mv.v.RemainingAttackCooldownTicks == 0)
                                        point = point + 100 - enemy.v.AerialDefence;
                                    if (enemy.v.SquaredAerialAttackRange > dst && enemy.v.RemainingAttackCooldownTicks == 0)
                                        point = point - (enemy.v.AerialDamage - mv.v.AerialDefence);
                                    if (mv.v.SquaredAerialAttackRange > dst && mv.v.RemainingAttackCooldownTicks > 0)
                                        temp = 1;
                                }
                                else
                                {
                                    if (enemy.v.SquaredAerialAttackRange > dst * 4)
                                        point = point - 100;
                                }

                            }
                            cntReman = cntReman + temp;
                        }

                        if (cntReman > MaxReman)
                            MaxReman = cntReman;

                        if (Bestpoint == 0 || Bestpoint < point)
                        {
                            Bestpoint = point;
                            moveX = xCenter + 8 * x;
                            moveY = yCenter + 8 * y;
                        }
                    }


                if (MaxReman > 30)
                {
                    Roatate(0.3);
                    waitMove = true;
                    return;
                }

                if (xCenter != moveX || yCenter != moveY)
                {
                    MoveToXY(moveX, moveY);
                    waitMove = true;
                }
                else
                {
                    Scale(0.1);
                    waitToNextCmd = 10;
                }
            }
            public void Fighter()
            {
                if (isMove() && waitMove)
                    return;

                double bestX = -1, bestY = -1;
                double bestPoint = 0;
                int myFigher = allObject.Where(i => i.isMe && i.v.Type == VehicleType.Fighter).Count();
                int enemyFigher = allObject.Where(i => !i.isMe && i.v.IsAerial).Count();

                double durablity = vencicleFormation.Min(i => i.v.Durability);

                double minMyDst = 0;
                double maxMyDst = 0;
                foreach (MyVencicle mv1 in vencicleFormation)
                    foreach (MyVencicle mv2 in vencicleFormation)

                        if (enemyFigher == 0 && durablity < 85)
                        {
                            foreach (myFormation form in formation.Where(i => i.venType == VehicleType.Arrv))
                            {
                                if (form.vencicleFormation.Count > 5)
                                {
                                    MoveToXY(form.xCenter, form.yCenter);
                                    waitToNextCmd = 120;
                                    return;
                                }
                            }
                        }

                if (vencicleFormation.Where(i => i.v.RemainingAttackCooldownTicks > 0).Count() > 5 && !isRotate)
                {
                    Fighter_2();
                    return;
                }



                foreach (MyVencicle hel in allObject.Where(i => i.isMe && i.v.Type == VehicleType.Helicopter))
                    foreach (MyVencicle fight in allObject.Where(i => !i.isMe && i.v.Type == VehicleType.Fighter))
                    {
                        double dst = getDistanceSQR(hel, fight);
                        if (dst < fight.v.SquaredAerialAttackRange + 5 * 5)
                        {
                            MoveToXYHalf(fight.v.X, fight.v.Y);
                            waitMove = true;
                            return;
                        }
                    }


                for (int x = -1; x < 2; x++)
                    for (int y = -1; y < 2; y++)
                    {
                        bool isBreak = false;
                        double point = 0;
                        bool isColis = false;
                        foreach (MyVencicle mvForm in vencicleFormation)
                        {
                            MyVencicle mv = mvForm;
                            mv.myX = mv.v.X + x * 32;
                            mv.myY = mv.v.Y + y * 32;

                            if (mv.myX < -15 || mv.myX > 1036 || mv.myY < -15 || mv.myY > 1026)
                            {
                                point = -100000;
                                isColis = true;
                                isBreak = true;
                                break;
                            }

                            foreach (MyVencicle obj in allObject.Where(i => i.v.Type != VehicleType.Arrv))
                            {
                                if (obj.isMe && !obj.v.IsAerial)
                                    continue;
                                if (obj.isMe && obj.groupNum == numGroup)
                                    continue;
                                double dst = getDistanceSQR(mv.myX, mv.myY, obj.v.X, obj.v.Y);
                                if (obj.isMe && obj.v.IsAerial && dst < 33 * 33)
                                {
                                    if (dst < 10 * 10)
                                    {
                                        Scale(1.2);
                                        waitMove = true;
                                        return;
                                    }
                                    point = -10000 + Math.Sqrt(dst);
                                    isBreak = true;
                                    isColis = true;
                                    break;
                                }
                                if (!obj.isMe)
                                {

                                    if (obj.v.Type == VehicleType.Ifv && dst < obj.v.SquaredAerialAttackRange + 25 * 25)
                                        point = point - 1000;
                                    if (obj.v.Type == VehicleType.Fighter && dst < mv.v.SquaredAerialAttackRange + 32 * 32)
                                        point = point + 5;
                                    if (obj.v.Type == VehicleType.Helicopter && dst < mv.v.SquaredAerialAttackRange + 32 * 32)
                                        point = point + 7;
                                }

                            }
                            if (isBreak)
                                break;
                        }
                        if (point == 0)
                        {
                            double bestDst = 0;
                            for (int mx = 0; mx < 32; mx++)
                                for (int my = 0; my < 32; my++)
                                {
                                    if (myMap[mx, my].cntEnemyHel > 0)
                                    {
                                        double dst = getDistanceSQR(myMap[mx, my].x, myMap[mx, my].y, xCenter + x * 32, yCenter + y * 32);
                                        if (dst < bestDst || bestDst == 0)
                                        {
                                            bestDst = dst;
                                            point = (2 * 1024 * 1024 - dst) / (2 * 1024 * 1024);
                                        }
                                    }

                                }
                        }

                        double dstForNearUnit = 0;
                        MyVencicle NearUnit = null;
                        foreach (MyVencicle mv in allObject.Where(i => i.isMe == false))
                        {
                            double dst = getDistanceSQR(xCenter, yCenter, mv.v.X, mv.v.Y);
                            if (dstForNearUnit < dst || dstForNearUnit == 0)
                            {
                                dstForNearUnit = dst;
                                if (dstForNearUnit < 100 * 100)
                                {
                                    NearUnit = mv;
                                    break;
                                }
                            }
                        }

                        if (point == 0 && ownConst.RemainingNuclearStrikeCooldownTicks < 100 && dstForNearUnit < 100 * 100 && NearUnit != null && dstForNearUnit > 0)
                        {
                            double bestDst = 0;
                            for (int mx = 0; mx < 32; mx++)
                                for (int my = 0; my < 32; my++)
                                {
                                    if (myMap[mx, my].cntMyAll == 0 && myMap[mx, my].cntEnemyAll == 0)
                                    {
                                        double dst = getDistanceSQR(myMap[mx, my].x, myMap[mx, my].y, xCenter + x * 32, yCenter + y * 32);
                                        double dst2 = getDistanceSQR(myMap[mx, my].x, myMap[mx, my].y, NearUnit.v.X, NearUnit.v.Y);
                                        if ((dst < bestDst || bestDst == 0) && dst > dst2)
                                        {
                                            bestDst = dst;
                                            point = (2 * 1024 * 1024 - dst) / (2 * 1024 * 1024);
                                        }
                                    }

                                }
                        }

                        if (point == 0)
                        {
                            double bestDst = 0;
                            for (int mx = 0; mx < 32; mx++)
                                for (int my = 0; my < 32; my++)
                                {
                                    if (myMap[mx, my].cntEnemyArial > 1)
                                    {
                                        double dst = getDistanceSQR(myMap[mx, my].x, myMap[mx, my].y, xCenter + x * 32, yCenter + y * 32);
                                        if (dst < bestDst || bestDst == 0)
                                        {
                                            bestDst = dst;
                                            point = (2 * 1024 * 1024 - dst) / (2 * 1024 * 1024);
                                        }
                                    }

                                }
                        }
                        if (point == 0)
                        {
                            double bestDst = 0;

                            foreach (myFormation f in formation.Where(i => i.vencicleFormation.Count(k => k.v.Type == VehicleType.Tank) > 10))
                            {
                                double dst = getDistanceSQR(f.xCenter, f.yCenter, xCenter + x * 32, yCenter + y * 32);
                                if (dst < bestDst || bestDst == 0)
                                {
                                    bestDst = dst;
                                    point = 2 * 1024 * 1024 - bestDst;
                                }
                            }
                        }

                        if (!isColis)
                            if (point > bestPoint || bestPoint == 0)
                            {
                                bestPoint = point;
                                bestX = xCenter + x * 32;
                                bestY = yCenter + y * 32;
                            }
                    }
                if (bestX > 0)
                {
                    MoveToXY(bestX, bestY);
                    waitMove = true;
                }
            }

            public void Helicopter()
            {

                if (isMove() && waitMove)
                    return;
                double bestX = -1, bestY = -1;
                double bestPoint = 0;
                double myDurablility = vencicleFormation.Average(i => i.v.Durability);


                if (vencicleFormation.Count(i => i.v.RemainingAttackCooldownTicks > 0) > 2)
                    isNeedRescale = true;

                myCell cellFighter = null;
                myCell cellmyIfv = null;

                double nearFigherDst = 0;
                double minDstCollis = 0;
                for (int x = 1; x < 31; x++)
                    for (int y = 1; y < 31; y++)
                    {
                        int cntEnemyFighter = myMap[x, y].cntEnemyFighter;
                        cntEnemyFighter = cntEnemyFighter + myMap[x - 1, y - 1].cntEnemyFighter;
                        cntEnemyFighter = cntEnemyFighter + myMap[x, y - 1].cntEnemyFighter;
                        cntEnemyFighter = cntEnemyFighter + myMap[x + 1, y - 1].cntEnemyFighter;
                        cntEnemyFighter = cntEnemyFighter + myMap[x + 1, y].cntEnemyFighter;
                        cntEnemyFighter = cntEnemyFighter + myMap[x + 1, y + 1].cntEnemyFighter;
                        cntEnemyFighter = cntEnemyFighter + myMap[x, y + 1].cntEnemyFighter;
                        cntEnemyFighter = cntEnemyFighter + myMap[x - 1, y + 1].cntEnemyFighter;
                        cntEnemyFighter = cntEnemyFighter + myMap[x - 1, y].cntEnemyFighter;

                        double dmgToFight = vencicleFormation.Count() * 10;
                        double dmgToMe = cntEnemyFighter * 60;

                        if (dmgToMe > dmgToFight)
                        {
                            double dst = getDistanceSQR(xCenter, yCenter, myMap[x, y].x, myMap[x, y].y);
                            if ((nearFigherDst == 0 || nearFigherDst > dst) && dst<32*32*8)
                            {
                                nearFigherDst = dst;
                                cellFighter = myMap[x, y];
                            }
                        }
                    }

                if (nearFigherDst > 0)//есть опасные самолеты
                {
                    double bestnearIfv = 0;
                    if (formation.Where(i => i.venType == VehicleType.Ifv && i.vencicleFormation.Count > 5)
                            .Count() > 0)
                    {
                        myFormation mf = formation.First(i =>
                            i.venType == VehicleType.Ifv && i.vencicleFormation.Count > 5);
                        double hideDst = nearFigherDst;
                        cellmyIfv = new myCell(mf.xCenter, mf.yCenter);

                        for (int x = -1; x < 2; x++)
                            for (int y = -1; y < 2; y++)
                            {
                                if (getDistanceSQR(cellFighter.x, cellFighter.y, mf.xCenter + x * 32,
                                        mf.yCenter + y * 32) > hideDst)
                                {
                                    hideDst = getDistanceSQR(cellFighter.x, cellFighter.y, mf.xCenter + x * 32, mf.yCenter + y * 32);
                                    cellmyIfv = new myCell(mf.xCenter + x * 32, mf.yCenter + y * 32);
                                }
                            }

                    }

                }

                int cntMyFight = allObject.Count(i => i.isMe == true && i.v.Type == VehicleType.Fighter);

                for (int x = -2; x < 3; x++)
                    for (int y = -2; y < 3; y++)
                    {
                        bool isBreak = false;
                        double point = 0;
                        minDstCollis = 0;
                        if (cellmyIfv != null)
                        {
                            double dstToIfv = getDistanceSQR(xCenter + x * 16, yCenter + y * 16, cellmyIfv.x, cellmyIfv.y);
                            point = (2 * 1024 * 1024 - dstToIfv) / (2 * 1024 * 1024);
                        }
                        else
                        {
                            foreach (MyVencicle mvForm in vencicleFormation)
                            {
                                MyVencicle mv = mvForm;
                                mv.myX = mv.v.X + x * 16;
                                mv.myY = mv.v.Y + y * 16;
                                int newMapX = (int)Math.Floor(mv.myX / 32);
                                int newMapY = (int)Math.Floor(mv.myY / 32);
                                if (newMapX < 0 || newMapX > 31 || newMapY < 0 || newMapY > 31)
                                {
                                    isBreak = true;
                                    point = point - 1000;
                                    break;
                                }


                                if (mv.myX < 5 || mv.myX > 1020 && mv.myY < 5 || mv.myY > 1020)
                                {
                                    point = point - 100000;
                                    isBreak = true;
                                    break;
                                }

                                foreach (MyVencicle obj in allObject)
                                {

                                    if (obj.isMe && !obj.v.IsAerial)
                                        continue;
                                    if (obj.isMe && obj.groupNum == numGroup)
                                        continue;
                                    double dst = getDistanceSQR(mv.myX, mv.myY, obj.v.X, obj.v.Y);
                                    if (obj.isMe && obj.v.IsAerial && dst < 45 * 45)
                                    {
                                        if (minDstCollis == 0 || minDstCollis > dst)
                                            minDstCollis = dst;
                                    }

                                    if (myMap[newMapX, newMapY].cntEnemyAll < 5 && myMap[newMapX, newMapY].cntEnemyAll > 0)
                                        point = point + 5;
                                    if (!obj.isMe && myMap[newMapX, newMapY].cntEnemyAll > 0)
                                    {

                                        if (obj.v.Type == VehicleType.Ifv && dst < obj.v.SquaredAerialAttackRange + 35 * 35)
                                            point = point - 1000;
                                        if (obj.v.Type == VehicleType.Fighter && dst < mv.v.SquaredAerialAttackRange + 120 * 120)
                                            point = point - 10000;
                                        if (obj.v.Type == VehicleType.Helicopter && dst < mv.v.SquaredAerialAttackRange + 40 * 40 && cntMyFight < 10)
                                            point = point + 1;
                                        if (obj.v.Type == VehicleType.Tank && dst < mv.v.SquaredAerialAttackRange + 20 * 20)
                                            point = point + 50;
                                        if (obj.v.Type == VehicleType.Arrv && dst < mv.v.SquaredAerialAttackRange + 20 * 20)
                                            point = point + 100;
                                    }

                                }
                                if (isBreak)
                                    break;
                            }

                        }
                        if (minDstCollis > 0)
                            point = point - 10000 + Math.Sqrt(minDstCollis);

                        if (point == 0 && allObject.Where(i => i.isMe && i.v.Type == VehicleType.Arrv).Count() > 5 && myDurablility < 80)
                        {
                            isNeedRescale = false;
                            if (isNeedRescale)
                                Scale(0.1);
                            double bestDst = 0;
                            for (int mx = 0; mx < 32; mx++)
                                for (int my = 0; my < 32; my++)
                                {
                                    if (myMap[mx, my].cntMyArrv > 15)
                                    {
                                        double dst = getDistanceSQR(myMap[mx, my].x, myMap[mx, my].y, xCenter + x * 16, yCenter + y * 16);
                                        if (dst < bestDst || bestDst == 0)
                                        {
                                            bestDst = dst;
                                            point = (2 * 1024 * 1024 - dst) / (2 * 1024 * 1024);
                                        }
                                    }

                                }
                        }

                        if (point == 999)
                        {
                            double bestDst = 0;
                            for (int mx = 0; mx < 32; mx++)
                                for (int my = 0; my < 32; my++)
                                {
                                    if ((myMap[mx, my].cntEnemyTank > 0 || myMap[mx, my].cntEnemyArrv > 0) && myMap[mx, my].cntMyIfv == 0 && myMap[mx, my].cntMyArrial == 0)
                                    {
                                        double dst = getDistanceSQR(myMap[mx, my].x, myMap[mx, my].y, xCenter + x * 16, yCenter + y * 16);
                                        if (dst < bestDst || bestDst == 0)
                                        {
                                            bestDst = dst;
                                            point = (2 * 1024 * 1024 - dst) / (2 * 1024 * 1024);
                                        }
                                    }

                                }
                        }

                        if (point == 0)
                        {
                            double bestDst = 0;
                            for (int mx = 0; mx < 32; mx++)
                                for (int my = 0; my < 32; my++)
                                {
                                    if (myMap[mx, my].cntEnemyArrv > 0 && myMap[mx, my].cntEnemyIvf == 0 && myMap[mx, my].cntEnemyArial == 0)
                                    {
                                        double dst = getDistanceSQR(myMap[mx, my].x, myMap[mx, my].y, xCenter + x * 16, yCenter + y * 16);
                                        if (dst < bestDst || bestDst == 0)
                                        {
                                            bestDst = dst;
                                            point = (2 * 1024 * 1024 - dst) / (2 * 1024 * 1024);
                                        }
                                    }

                                }
                        }

                        if (point == 0)
                        {
                            double bestDst = 0;
                            for (int mx = 0; mx < 32; mx++)
                                for (int my = 0; my < 32; my++)
                                {
                                    if (myMap[mx, my].cntEnemyTank > 0 && myMap[mx, my].cntEnemyIvf == 0 && myMap[mx, my].cntEnemyArial == 0)
                                    {
                                        double dst = getDistanceSQR(myMap[mx, my].x, myMap[mx, my].y, xCenter + x * 16, yCenter + y * 16);
                                        if (dst < bestDst || bestDst == 0)
                                        {
                                            bestDst = dst;
                                            point = (2 * 1024 * 1024 - dst) / (2 * 1024 * 1024);
                                        }
                                    }

                                }
                        }

                        if (point == 0)
                        {
                            double bestDst = 0;
                            for (int mx = 0; mx < 32; mx++)
                                for (int my = 0; my < 32; my++)
                                {
                                    if (myMap[mx, my].cntEnemyTank > 0 && myMap[mx, my].cntEnemyIvf == 0 && myMap[mx, my].cntEnemyArial == 0)
                                    {
                                        double dst = getDistanceSQR(myMap[mx, my].x, myMap[mx, my].y, xCenter + x * 16, yCenter + y * 16);
                                        if (dst < bestDst || bestDst == 0)
                                        {
                                            bestDst = dst;
                                            point = (2 * 1024 * 1024 - dst) / (2 * 1024 * 1024);
                                        }
                                    }

                                }
                        }


                        if (point == 0)
                        {
                            double bestDst = 0;

                            foreach (myFormation f in formation.Where(i => i.vencicleFormation.Count(k => k.v.Type == VehicleType.Ifv) > 10))
                            {
                                double dst = getDistanceSQR(f.xCenter, f.yCenter, xCenter + x * 16, yCenter + y * 16);
                                if (dst < bestDst || bestDst == 0)
                                {
                                    bestDst = dst;
                                    point = 2 * 1024 - Math.Sqrt(bestDst);
                                }
                            }
                        }




                        if (point > bestPoint || (bestPoint == 0 && bestX < 0) && (x != 0 || y != 0))
                        {
                            bestPoint = point;
                            bestX = xCenter + x * 16;
                            bestY = yCenter + y * 16;
                        }
                    }
                if (bestX > 0)
                {
                    MoveToXY(bestX, bestY);
                    waitMove = true;
                }
            }


            public double getDistanceSQR(double x1, double y1, double x2, double y2)
            {
                return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
            }

            public double getDistanceSQR(MyVencicle mv1, MyVencicle mv2)
            {
                return (mv1.v.X - mv2.v.X) * (mv1.v.X - mv2.v.X) + (mv1.v.Y - mv2.v.Y) * (mv1.v.Y - mv2.v.Y);
            }


            public VehicleType GetVehicleType()
            {
                return vencicleFormation.First().v.Type;
            }
            public void SelectGroup()
            {
                Move m = new Model.Move();
                m.Action = ActionType.ClearAndSelect;
                m.Group = numGroup;
                poolMove.Add(m);
            }
            public bool isMove()
            {
                int cntMove = vencicleFormation.Where(i => i.isMove).Count();
                int cntNotMove = vencicleFormation.Where(i => i.isMove == false).Count();

                if (isWaitFullMove)
                    if (cntMove > 0)
                        return true;

                if (cntMove > cntNotMove * 2)
                    return true;
                return false;
            }
            public bool isSelectNow()
            {
                if (vencicleFormation.Where(i => i.v.IsSelected).Count() > 0)
                    return true;
                return false;
            }
            public void MoveToXY(double x, double y)
            {
                isWaitFullMove = false;
                lastTick = worldConst.TickIndex;
                if ((venType == VehicleType.Helicopter || venType == VehicleType.Fighter) && isNuclear == false)
                    lastTick = numGroup;
                tarX = x;
                tarY = y;
                targetX = (int)Math.Floor(x / 32);
                targetY = (int)Math.Floor(y / 32);
                if (targetX < 0 || targetX > 31 || targetY < 0 || targetY > 31)
                {
                    waitToNextCmd = 10;
                    return;
                }
                double minSpeed = vencicleFormation.Min(i => i.maxSpeed);
                Move m = new Model.Move();
                m.Action = ActionType.Move;
                m.X = x - xCenter;
                m.Y = y - yCenter;
                //                if (venType == VehicleType.Fighter || venType == VehicleType.Helicopter)
                //                    m.MaxSpeed = minSpeed;
                if (Math.Abs(m.X) > 5 || Math.Abs(m.Y) > 5)
                    poolMove.Add(m);
            }
            public void MoveToXYHalf(double x, double y)
            {
                isWaitFullMove = false;
                lastTick = worldConst.TickIndex;
                if ((venType == VehicleType.Helicopter || venType == VehicleType.Fighter) && isNuclear == false)
                    lastTick = numGroup;
                tarX = x;
                tarY = y;
                targetX = (int)Math.Floor(x / 32);
                targetY = (int)Math.Floor(y / 32);
                if (targetX < 0 || targetX > 31 || targetY < 0 || targetY > 31)
                {
                    waitToNextCmd = 10;
                    return;
                }
                double minSpeed = vencicleFormation.Min(i => i.maxSpeed);
                Move m = new Model.Move();
                m.Action = ActionType.Move;
                m.X = x - xCenter;
                m.Y = y - yCenter;

                double len = Math.Sqrt(m.X * m.X + m.Y * m.Y);
                if (len > 45)
                {
                    m.X = m.X / len * 32;
                    m.Y = m.Y / len * 32;
                }

                //                if (venType == VehicleType.Fighter || venType == VehicleType.Helicopter)
                //                    m.MaxSpeed = minSpeed;
                if (Math.Abs(m.X) > 5 || Math.Abs(m.Y) > 5)
                    poolMove.Add(m);
            }

        }

        public class myCell
        {
            public double top, left, bottom, right, x, y;
            public int mapX, mapY;
            public int notVisible;

            public bool isCloseArial;
            public bool isCloseGroud;


            public List<MyVencicle> cellVencicles;
            public Facility f;
            public int cntEnemyForNuclear;
            public int cntEnemyGround;
            public int cntEnemyArial;
            public int cntEnemyAll;
            public int cntEnemyTank;
            public int cntEnemyIvf;
            public int cntEnemyArrv;
            public int cntEnemyFighter;
            public int cntEnemyHel;

            public int cntMyAll;
            public int cntMyTank;
            public int cntMyArrv;
            public int cntMyIfv;
            public int cntMyHel;
            public int cntMyFigher;
            public int cntMyGround;
            public int cntMyArrial;

            public double potencial;

            public myCell(double xCenter, double yCenter)
            {
                x = xCenter; y = yCenter;
                top = yCenter - 16;
                left = xCenter - 16;
                bottom = y + 16;
                right = x + 16;

                mapX = (int)Math.Floor(xCenter / 32);
                mapY = (int)Math.Floor(yCenter / 32);

                isCloseArial = false;
                isCloseGroud = false;
                potencial = 0;
                notVisible = 0;
            }



        }


        public class MyVencicle
        {
            public Vehicle v;
            public int mapX, mapY;
            public double visionRange;
            public double maxSpeed;
            public bool isMove;
            public double prevX;
            public double prevY;
            public int groupNum;
            public bool isMe;
            public double myX, myY;
            public double speedX, speedY;
            public bool isNuclear;




            public MyVencicle(Vehicle vencicle)
            {
                v = vencicle;
                myX = v.X;
                myY = v.Y;
                prevX = vencicle.X;
                prevY = vencicle.Y;
                isMe = v.PlayerId == meConst.Id;
                groupNum = 0;
                speedX = 0;
                speedY = 0;
                isNuclear = false;
            }
            public void update(Vehicle vencicle)
            {
                v = vencicle;
                myX = v.X;
                myY = v.Y;
                mapX = (int)Math.Floor(v.X / 32);
                mapY = (int)Math.Floor(v.Y / 32);
                visionRange = v.VisionRange;
                maxSpeed = v.MaxSpeed;
                if (v.IsAerial)
                {
                    WeatherType wt = worldConst.WeatherByCellXY[mapX][mapY];
                    if (wt == WeatherType.Cloud)
                    {
                        visionRange = visionRange * gameConst.CloudWeatherVisionFactor;
                        maxSpeed = maxSpeed * gameConst.CloudWeatherSpeedFactor;
                    }
                    if (wt == WeatherType.Rain)
                    {
                        visionRange = visionRange * gameConst.RainWeatherSpeedFactor;
                        maxSpeed = maxSpeed * gameConst.RainWeatherSpeedFactor;
                    }
                }
                else
                {
                    TerrainType tt = worldConst.TerrainByCellXY[mapX][mapY];
                    if (tt == TerrainType.Forest)
                    {
                        visionRange = visionRange * gameConst.ForestTerrainVisionFactor;
                        maxSpeed = maxSpeed * gameConst.ForestTerrainSpeedFactor;
                    }
                    if (tt == TerrainType.Swamp)
                    {
                        visionRange = visionRange * gameConst.SwampTerrainVisionFactor;
                        maxSpeed = maxSpeed * gameConst.SwampTerrainSpeedFactor;
                    }
                }
                if (prevX != v.X || prevY != v.Y)
                    isMove = true;
                else
                    isMove = false;
                speedX = v.X - prevX;
                speedY = v.Y - prevY;
                prevX = v.X;
                prevY = v.Y;
                if (v.Groups.Count() > 0)
                    groupNum = v.Groups[0];
            }
        }


        public void unionGroup()
        {
            int main, second;
            foreach (myFormation f1 in formation)
                foreach (myFormation f2 in formation.Where(i => i.numGroup != f1.numGroup))
                {
                    if (f1.isArial)
                        break;
                    if (f2.isArial)
                        continue;
                    if (f1.vencicleFormation.Count == 0)
                        break;
                    if (f2.vencicleFormation.Count == 0)
                        continue;
                    if (f1.venType == f2.venType)
                    {
                        double dst = getDistanceSQR(f1.xCenter, f1.yCenter, f2.xCenter, f2.yCenter);
                        if (dst < 55 * 55)
                        {
                            if (f1.vencicleFormation.Count > f2.vencicleFormation.Count)
                            {
                                main = f1.numGroup;
                                second = f2.numGroup;
                            }
                            else
                            {
                                main = f2.numGroup;
                                second = f1.numGroup;
                            }

                            Move m = new Model.Move();
                            m.Action = ActionType.ClearAndSelect;
                            m.Group = second;
                            pool.Add(m);
                            m = new Model.Move();
                            m.Action = ActionType.Dismiss;
                            m.Group = second;
                            pool.Add(m);
                            m = new Model.Move();
                            m.Action = ActionType.Assign;
                            m.Group = main;
                            pool.Add(m);
                            return;
                        }
                    }
                }



        }
        public void Fac()
        {
            foreach (Facility fac in worldConst.Facilities.Where(i => i.OwnerPlayerId == meConst.Id && i.Type == FacilityType.VehicleFactory))
            {
                if (fac.VehicleType == null)
                {
                    Move m = new Move();
                    m.Action = ActionType.SetupVehicleProduction;
                    m.VehicleType = getBestVenType();
                    m.FacilityId = fac.Id;
                    pool.Add(m);
                }
            }
            foreach (Facility fac in worldConst.Facilities.Where(i => i.OwnerPlayerId == meConst.Id))
            {
                int cntMyInit = 0;
                foreach (MyVencicle mv in allObject.Where(i => i.isMe && i.groupNum == 0))
                {
                    if (mv.v.X >= fac.Left && mv.v.X <= fac.Left + 64 && mv.v.Y >= fac.Top && mv.v.Y <= fac.Top + 64)
                        cntMyInit++;
                }




                if (cntMyInit > 33 || (fac.VehicleType == VehicleType.Fighter && cntMyInit > 22))
                {
                    Move m = new Model.Move();
                    m.Action = ActionType.ClearAndSelect;
                    m.Left = fac.Left;
                    m.Right = fac.Left + 64;
                    m.Top = fac.Top;
                    m.Bottom = fac.Top + 64;
                    m.VehicleType = fac.VehicleType;
                    pool.Add(m);
                    m = new Model.Move();
                    m.Action = ActionType.Scale;
                    m.X = fac.Left + 32;
                    m.Y = fac.Top + 32;
                    m.Factor = 0.1;
                    pool.Add(m);
                    m = new Model.Move();
                    m.Group = GetNextGroupNum();
                    m.Action = ActionType.Assign;
                    pool.Add(m);
                    myFormation form = new myFormation();
                    form.numGroup = m.Group;
                    form.waitMove = true;
                    form.waitToNextCmd = 20;
                    formation.Add(form);
                    m = new Move();
                    m.Action = ActionType.SetupVehicleProduction;
                    m.VehicleType = getBestVenType();
                    m.FacilityId = fac.Id;
                    pool.Add(m);
                }
            }


        }

        public VehicleType getBestVenType()
        {
            int cntFighter = allObject.Where(i => !i.isMe && i.v.Type == VehicleType.Fighter).Count();
            int cntHel = allObject.Where(i => !i.isMe && i.v.Type == VehicleType.Helicopter).Count();
            int cntTank = allObject.Where(i => !i.isMe && i.v.Type == VehicleType.Tank).Count();
            int cntIfv = allObject.Where(i => !i.isMe && i.v.Type == VehicleType.Ifv).Count();

            int cntMax = Math.Max(Math.Max(Math.Max(cntFighter, cntHel), cntTank), cntIfv);

            int cntMyTank = allObject.Where(i => i.isMe && i.v.Type == VehicleType.Tank).Count();
            int cntMyIfv = allObject.Where(i => i.isMe && i.v.Type == VehicleType.Ifv).Count();
            int cntMyHel = allObject.Where(i => i.isMe && i.v.Type == VehicleType.Helicopter).Count();
            int cntMyFigh = allObject.Where(i => i.isMe && i.v.Type == VehicleType.Fighter).Count();


            int cntFacFigh = worldConst.Facilities.Where(i => i.OwnerPlayerId == meConst.Id && i.VehicleType == VehicleType.Fighter).Count();
            int cntFacTank = worldConst.Facilities.Where(i => i.OwnerPlayerId == meConst.Id && i.VehicleType == VehicleType.Tank).Count();
            int cntFacIfv = worldConst.Facilities.Where(i => i.OwnerPlayerId == meConst.Id && i.VehicleType == VehicleType.Ifv).Count();


            //            if (cntMyFigh <= 20 && cntFacFigh == 0)
            //                return VehicleType.Fighter;

            //            if (cntMyHel <= 20 && cntMyHel == 0)
            //                return VehicleType.Helicopter;



            if (cntFacTank < 5)
                return VehicleType.Tank;

            if (cntFacIfv < 3)
                return VehicleType.Ifv;

            return VehicleType.Helicopter;

            if (cntMyTank == 0 && cntFacTank == 0)
                return VehicleType.Tank;

            if (cntFacTank > cntFacIfv && cntFacTank > 2)
                return VehicleType.Ifv;

            if (cntTank >= cntMax && cntMax > 0)
                return VehicleType.Tank;
            if (cntIfv >= cntMax)
                return VehicleType.Tank;
            return VehicleType.Ifv;
        }

        public int GetFormationInPos(int pos)
        {
            foreach (myFormation f in formation.Where(i => i.isArial == false))
            {
                if (pos == 1 && f.xCenter > 18 && f.xCenter < 72 && f.yCenter > 18 && f.yCenter < 72)
                    return f.numGroup;
                if (pos == 2 && f.xCenter > 92 && f.xCenter < 146 && f.yCenter > 18 && f.yCenter < 72)
                    return f.numGroup;
                if (pos == 3 && f.xCenter > 166 && f.xCenter < 220 && f.yCenter > 18 && f.yCenter < 72)
                    return f.numGroup;
                if (pos == 4 && f.xCenter > 18 && f.xCenter < 72 && f.yCenter > 92 && f.yCenter < 146)
                    return f.numGroup;
                if (pos == 5 && f.xCenter > 92 && f.xCenter < 146 && f.yCenter > 92 && f.yCenter < 146)
                    return f.numGroup;
                if (pos == 6 && f.xCenter > 166 && f.xCenter < 220 && f.yCenter > 92 && f.yCenter < 146)
                    return f.numGroup;
                if (pos == 7 && f.xCenter > 18 && f.xCenter < 72 && f.yCenter > 166 && f.yCenter < 220)
                    return f.numGroup;
                if (pos == 8 && f.xCenter > 92 && f.xCenter < 146 && f.yCenter > 166 && f.yCenter < 220)
                    return f.numGroup;
                if (pos == 9 && f.xCenter > 166 && f.xCenter < 220 && f.yCenter > 166 && f.yCenter < 220)
                    return f.numGroup;
            }

            return 0;
        }

        public void step1()
        {
            int k = 0;
            int num = 0;
            int num1, num2, num3, num4, num5, num6, num7, num8, num9;
            num1 = GetFormationInPos(1);
            num2 = GetFormationInPos(2);
            num3 = GetFormationInPos(3);
            num4 = GetFormationInPos(4);
            num5 = GetFormationInPos(5);
            num6 = GetFormationInPos(6);
            num7 = GetFormationInPos(7);
            num8 = GetFormationInPos(8);
            num9 = GetFormationInPos(9);

            myFormation f1 = null;
            myFormation f2 = null;
            myFormation f3 = null;

            myFormation f9 = null;

            myFactilies fac9 = null;

            double dstToCenter = 0;
            foreach (myFormation f in formation)
            {
                if (k == 0 && f.isArial == false)
                {
                    dstToCenter = getDistanceSQR(512, 512, f.xCenter, f.yCenter);
                    f9 = f;
                    f1 = f; k++; continue;
                }
                if (k == 1 && f.isArial == false)
                {
                    if (dstToCenter > getDistanceSQR(512, 512, f.xCenter, f.yCenter))
                    {
                        dstToCenter = getDistanceSQR(512, 512, f.xCenter, f.yCenter);
                        f9 = f;
                    }
                    f2 = f; k++; continue;
                }
                if (k == 2 && f.isArial == false)
                {
                    if (dstToCenter > getDistanceSQR(512, 512, f.xCenter, f.yCenter))
                    {
                        dstToCenter = getDistanceSQR(512, 512, f.xCenter, f.yCenter);
                        f9 = f;
                    }
                    f3 = f; k++; continue;
                }
            }
            if (k < 3)
            {
                step = "step2";
                return;
            }

            dstToCenter = 0;
            foreach (myFactilies fac in myFacList)
            {
                if (fac.f.Left > 220 && fac.f.Top > 220)
                {
                    if (getDistanceSQR(fac.xCenter, fac.yCenter, f9.xCenter, f9.yCenter) < dstToCenter || dstToCenter == 0)
                    {
                        fac9 = fac;
                        dstToCenter = getDistanceSQR(fac.xCenter, fac.yCenter, f9.xCenter, f9.yCenter);
                    }
                }
            }

            if (fac9 != null)
            {
                f9.myFac = fac9;
                f9.MoveToXY(fac9.xCenter, fac9.yCenter);
                f9.waitMove = true;
            }

            step = "step2";
        }

        public void Move(Player me, World world, Game game, Move move)
        {


            if (world.TickIndex >= 1810)
            {
                   Console.WriteLine(world.TickIndex);
            }

            Player own = world.Players.First(i => !i.IsMe);

            worldConst = world;
            meConst = me;
            gameConst = game;


            if (world.TickIndex == 1049)
            {
                int kkk = 0;
            }

            if (world.TickIndex == 0)
            {
                InitWorld(me, world, game);
                globalWait = 0;
                //vc = new VisualClient("127.0.0.1", 13579);


            }

            //vc.BeginPost();

            updateWorld(world);

            if (me.RemainingActionCooldownTicks > 0)
                return;

            if (pool.Count == 0 && step == "Step1")
            {
                step1();
            }
            if (pool.Count() == 0 && formation.Count > 0)
                unionGroup();

            if (pool.Count() == 0)
                Fac();
            NuclearStrike();

            if (pool.Count() == 0 && (me.NextNuclearStrikeTickIndex < 0 || own.NextNuclearStrikeTickIndex > 0))
                foreach (myFormation f in formation.OrderBy(i => i.lastTick))
                {
                    createFormationNuclear();
                    if (pool.Count() > 0)
                        break;
                    if (f.poolMove.Count() == 0)
                        f.Main();
                    foreach (Move m in f.poolMove)
                    {
                        if (!f.isSelectNow())
                        {
                            Move mm = new Model.Move();
                            mm.Action = ActionType.ClearAndSelect;
                            mm.Group = f.numGroup;
                            pool.Add(mm);
                        }
                        pool.Add(m);
                    }
                    f.poolMove = new List<Model.Move>();
                    if (pool.Count() > 0)
                        break;

                }

            //vc.EndPost();

            if (pool.Count() > 0)
            {
                move.Action = pool[0].Action;
                move.Group = pool[0].Group;
                move.Factor = pool[0].Factor;
                move.VehicleType = pool[0].VehicleType;
                move.Left = pool[0].Left;
                move.Right = pool[0].Right;
                move.Top = pool[0].Top;
                move.Bottom = pool[0].Bottom;
                move.X = pool[0].X;
                move.Y = pool[0].Y;
                move.MaxSpeed = pool[0].MaxSpeed;
                move.MaxAngularSpeed = pool[0].MaxAngularSpeed;
                move.Angle = pool[0].Angle;
                move.VehicleId = pool[0].VehicleId;
                move.FacilityId = pool[0].FacilityId;
                pool.Remove(pool[0]);
                return;
            }


            if (step == "Init")
            {
                InitFormation();
                return;
            }


        }




        public void createFormation(VehicleType vt)
        {
            double x1, x2, y1, y2, xcenter, ycenter;
            x1 = allObject.Where(i => i.isMe && i.v.Type == vt).Min(i => i.v.X) - 1;
            x2 = allObject.Where(i => i.isMe && i.v.Type == vt).Max(i => i.v.X) + 1;
            y1 = allObject.Where(i => i.isMe && i.v.Type == vt).Min(i => i.v.Y) - 1;
            y2 = allObject.Where(i => i.isMe && i.v.Type == vt).Max(i => i.v.Y) + 1;

            xcenter = x1 + (x2 - x1) * 0.5;
            ycenter = y1 + (y2 - y1) * 0.5;


            Move m;
            myFormation f;

            if (vt == VehicleType.Fighter)
            {
                m = new Move();
                m.Action = ActionType.ClearAndSelect;
                m.VehicleType = vt;
                m.Left = x1;
                m.Top = y1;
                m.Right = x2;
                m.Bottom = y2;
                pool.Add(m);
                m = new Move();
                m.Action = ActionType.Assign;
                m.Group = GetNextGroupNum();
                f = new myFormation();
                f.numGroup = m.Group;
                formation.Add(f);
                pool.Add(m);
                m = new Move();
                m.Action = ActionType.Scale;
                m.X = xcenter;
                m.Y = ycenter;
                m.Factor = 0.1;
                pool.Add(m);
                return;
            }


            m = new Move();
            m.Action = ActionType.ClearAndSelect;
            m.VehicleType = vt;
            m.Left = x1;
            m.Top = y1;
            m.Right = xcenter;
            m.Bottom = ycenter;
            pool.Add(m);
            m = new Move();
            m.Action = ActionType.Assign;
            m.Group = GetNextGroupNum();
            f = new myFormation();
            f.numGroup = m.Group;
            formation.Add(f);
            pool.Add(m);
            m = new Move();
            m.Action = ActionType.Scale;
            m.X = x1 + (xcenter - x1) * 0.5;
            m.Y = y1 + (ycenter - y1) * 0.5;
            m.Factor = 0.1;
            pool.Add(m);



            m = new Move();
            m.Action = ActionType.ClearAndSelect;
            m.VehicleType = vt;
            m.Left = xcenter;
            m.Top = y1;
            m.Right = x2;
            m.Bottom = ycenter;
            pool.Add(m);
            m = new Move();
            m.Action = ActionType.Assign;
            m.Group = GetNextGroupNum();
            f = new myFormation();
            f.numGroup = m.Group;
            formation.Add(f);
            pool.Add(m);
            m = new Move();
            m.Action = ActionType.Scale;
            m.X = xcenter + (x2 - xcenter) * 0.5;
            m.Y = y1 + (ycenter - y1) * 0.5;
            m.Factor = 0.1;
            pool.Add(m);


            m = new Move();
            m.Action = ActionType.ClearAndSelect;
            m.VehicleType = vt;
            m.Left = xcenter;
            m.Top = ycenter;
            m.Right = x2;
            m.Bottom = y2;
            pool.Add(m);
            m = new Move();
            m.Action = ActionType.Assign;
            m.Group = GetNextGroupNum();
            f = new myFormation();
            f.numGroup = m.Group;
            formation.Add(f);
            pool.Add(m);
            m = new Move();
            m.Action = ActionType.Scale;
            m.X = xcenter + (x2 - xcenter) * 0.5;
            m.Y = ycenter + (y2 - ycenter) * 0.5;
            m.Factor = 0.1;
            pool.Add(m);


            m = new Move();
            m.Action = ActionType.ClearAndSelect;
            m.VehicleType = vt;
            m.Left = x1;
            m.Top = ycenter;
            m.Right = xcenter;
            m.Bottom = y2;
            pool.Add(m);
            m = new Move();
            m.Action = ActionType.Assign;
            m.Group = GetNextGroupNum();
            f = new myFormation();
            f.numGroup = m.Group;
            formation.Add(f);
            pool.Add(m);
            m = new Move();
            m.Action = ActionType.Scale;
            m.X = x1 + (xcenter - x1) * 0.5;
            m.Y = ycenter + (y2 - ycenter) * 0.5;
            m.Factor = 0.1;
            pool.Add(m);



        }

        public void createFormation_v2(VehicleType vt)
        {
            double x1, x2, y1, y2, xcenter, ycenter;
            x1 = allObject.Where(i => i.isMe && i.v.Type == vt).Min(i => i.v.X) - 1;
            x2 = allObject.Where(i => i.isMe && i.v.Type == vt).Max(i => i.v.X) + 1;
            y1 = allObject.Where(i => i.isMe && i.v.Type == vt).Min(i => i.v.Y) - 1;
            y2 = allObject.Where(i => i.isMe && i.v.Type == vt).Max(i => i.v.Y) + 1;

            xcenter = x1 + (x2 - x1) * 0.5;
            ycenter = y1 + (y2 - y1) * 0.5;


            Move m;
            myFormation f;


            if (vt == VehicleType.Fighter || vt == VehicleType.Helicopter)
            {
                m = new Move();
                m.Action = ActionType.ClearAndSelect;
                m.VehicleType = vt;
                m.Left = x1;
                m.Top = y1;
                m.Right = x2;
                m.Bottom = y2;
                pool.Add(m);
                m = new Move();
                m.Action = ActionType.Assign;
                m.Group = GetNextGroupNum();
                f = new myFormation();
                f.numGroup = m.Group;
                formation.Add(f);
                pool.Add(m);
                m = new Move();
                m.Action = ActionType.Scale;
                m.X = xcenter;
                m.Y = ycenter;
                m.Factor = 0.4;
                if (vt == VehicleType.Fighter)
                {
                    m.Factor = 0.4;
                }
                pool.Add(m);
                return;
            }

            m = new Move();
            m.Action = ActionType.ClearAndSelect;
            m.VehicleType = vt;
            m.Left = x1;
            m.Top = y1;
            m.Right = x2;
            m.Bottom = y2;
            pool.Add(m);
            m = new Move();
            m.Action = ActionType.Assign;
            m.Group = GetNextGroupNum();
            f = new myFormation();
            f.numGroup = m.Group;
            formation.Add(f);
            pool.Add(m);
            m = new Move();
            m.Action = ActionType.Scale;
            m.X = xcenter;
            m.Y = ycenter;
            m.Factor = 0.1;
            pool.Add(m);

        }

        public void createFormationNuclear()
        {
            if (pool.Count > 0)
                return;
            Move m = new Move();
            myFormation fNuc = null;
            if (formation.Count > 0)
                fNuc = formation.Find(i => i.isNuclear == true);
            if (fNuc != null)
                if (fNuc.vencicleFormation.Count > 0)
                    return;
            if (fNuc == null)
            {
                fNuc = new myFormation();
                fNuc.isNuclear = true;
                fNuc.numGroup = GetNextGroupNum();
                formation.Add(fNuc);
            }
            MyVencicle nucVen = null;
            double bestDst = 0;
            foreach (MyVencicle mv in allObject.Where(i => i.isMe && i.v.Type == VehicleType.Fighter))
            {
                double dst = getDistanceSQR(mv.v.X, mv.v.Y, 1024, 1024);
                if (bestDst == 0 || bestDst > dst)
                {
                    bestDst = dst;
                    nucVen = mv;
                }
            }
            if (nucVen == null)
                return;

            foreach (MyVencicle mv in allObject.Where(i => i.isMe == false && i.v.IsAerial))
            {
                if (getDistanceSQR(nucVen.v.X, nucVen.v.Y, mv.v.X, mv.v.Y) < 32 * 32 * 4)
                    return;
            }

            Console.WriteLine("Nuc=" + nucVen.v.Id);

            m = new Move();
            m.Action = ActionType.ClearAndSelect;
            m.Left = nucVen.v.X - 2;
            m.Right = nucVen.v.X + 2;
            m.Top = nucVen.v.Y - 2;
            m.Bottom = nucVen.v.Y + 2;
            pool.Add(m);
            if (nucVen.v.Groups.Length > 0)
            {
                m = new Move();
                m.Action = ActionType.Dismiss;
                m.Group = nucVen.groupNum;
                pool.Add(m);
            }
            m = new Move();
            m.Action = ActionType.Assign;
            m.Group = fNuc.numGroup;
            pool.Add(m);

        }

        public void InitFormation()
        {
            createFormation_v2(VehicleType.Ifv);
            createFormation_v2(VehicleType.Arrv);
            createFormation_v2(VehicleType.Tank);
            createFormation_v2(VehicleType.Fighter);
            createFormation_v2(VehicleType.Helicopter);
            step = "Step1";
        }


        public double getDistanceSQR(double x1, double y1, double x2, double y2)
        {
            double res = (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
            return res;
        }

        public int GetNextGroupNum()
        {
            return formation.Count() + 1;
        }

        public void InitWorld(Player me, World world, Game game)
        {
            worldConst = world;
            meConst = me;
            gameConst = game;
            allObject = new List<MyVencicle>();
            myMap = new myCell[32, 32];
            pool = new List<Move>();
            formation = new List<myFormation>();
            step = "Init";
            myFacList = new List<myFactilies>();
            foreach (Facility f in world.Facilities)
            {
                myFactilies myF = new myFactilies(f);
                myFacList.Add(myF);
            }

        }


        public void NuclearStrike()
        {
            if (meConst.RemainingNuclearStrikeCooldownTicks > 0)
                return;
            if (ownConst.NextNuclearStrikeTickIndex > 0)
                return;
            if (pool.Count() > 0)
                return;

            MyVencicle bestMv = null;
            int cntEnemyVisible = 0;
            foreach (MyVencicle mv in allObject.Where(i => i.isMe && i.isMove == false))
            {
                double visionRange = mv.visionRange * mv.visionRange;
                int cnt = 0;
                foreach (MyVencicle enemy in allObject.Where(i => !i.isMe))
                {
                    double dst = getDistanceSQR(mv.v.X, mv.v.Y, enemy.v.X, enemy.v.Y);
                    if (dst < enemy.v.AerialAttackRange * enemy.v.AerialAttackRange + 30 * enemy.maxSpeed * 30 * enemy.maxSpeed)
                    {
                        cnt = 0;
                        break;
                    }
                    if (dst < visionRange)
                    {
                        if (enemy.v.IsAerial == false)
                            cnt++;
                        cnt++;
                    }

                }
                if (cnt > cntEnemyVisible)
                {
                    cntEnemyVisible = cnt;
                    bestMv = mv;
                }
            }

            if (bestMv != null && cntEnemyVisible > 15)
            {
                double visionRange = bestMv.visionRange;
                Console.WriteLine(bestMv.v.Id);
                double nuclearX = 0, nuclearY = 0;
                double bestDmg = 0;
                for (double x = -visionRange; x < visionRange; x = x + 4)
                    for (double y = -visionRange; y < visionRange; y = y + 4)
                    {
                        double nucX = bestMv.v.X + x;
                        double nucY = bestMv.v.Y + y;

                        if (getDistanceSQR(0, 0, x, y) < 50 * 50 || getDistanceSQR(0, 0, x, y) > visionRange * visionRange)
                            continue;

                        double dmg = 0;
                        foreach (MyVencicle mv in allObject)
                        {
                            double dst = getDistanceSQR(mv.v.X, mv.v.Y, nucX, nucY);
                            if (mv.isMe && dst < 50 * 50)
                            {
                                dmg = dmg - 400;
                            }
                            if (!mv.isMe && dst < 50 * 50)
                            {

                                if (mv.v.Durability - 99 * (50 * 50 - dst) / (50 * 50) < 0)
                                    dmg = dmg + 100;
                                dmg = dmg + (99 * (50 * 50 - dst) / (50 * 50));
                            }
                        }
                        if (dmg > bestDmg && dmg > 100)
                        {
                            bestDmg = dmg;
                            nuclearX = nucX;
                            nuclearY = nucY;
                        }
                    }

                if (nuclearX > 0)
                {
                    pool = new List<Model.Move>();
                    Move m;
                    m = new Model.Move();
                    m.Action = ActionType.TacticalNuclearStrike;
                    m.X = nuclearX;
                    m.Y = nuclearY;
                    m.VehicleId = bestMv.v.Id;
                    pool.Add(m);
                    if (bestMv.groupNum > 0)
                    {
                        m = new Model.Move();
                        m.Action = ActionType.ClearAndSelect;
                        m.Group = bestMv.groupNum;
                        pool.Add(m);
                        m = new Model.Move();
                        m.Action = ActionType.Move;
                        m.X = 0;
                        m.Y = 0;
                        pool.Add(m);
                    }
                }
            }

        }

        public void updateWorld(World world)
        {
            ownConst = world.Players.First(i => !i.IsMe);

            foreach (MyVencicle mv in allObject)
                mv.isMove = false;
            foreach (Vehicle v in world.NewVehicles)
            {

                MyVencicle mv = allObject.Find(item => item.v.Id == v.Id);
                if (mv == null)
                {
                    mv = new MyVencicle(v);
                    mv.update(v);
                    allObject.Add(mv);
                }
                else
                    mv.update(v);
            }
            foreach (VehicleUpdate vu in world.VehicleUpdates)
            {


                MyVencicle mv = allObject.Find(item => item.v.Id == vu.Id);
                if (mv != null)
                {
                    Vehicle uptVen = new Vehicle(mv.v, vu);
                    mv.update(uptVen);
                }
                if (vu.Durability <= 0)
                {
                    allObject.RemoveAll(item => item.v.Id == vu.Id);
                }
            }


            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                {
                    myCell prev = myMap[x, y];
                    myMap[x, y] = new myCell(x * 32 + 16, y * 32 + 16);
                }


            foreach (MyVencicle mv in allObject)
            {


                if (mv.isMe)
                {
                    if (mv.v.IsAerial)
                        myMap[mv.mapX, mv.mapY].cntMyArrial++;
                    else
                        myMap[mv.mapX, mv.mapY].cntMyGround++;

                    myMap[mv.mapX, mv.mapY].cntMyAll++;
                    if (mv.v.Type == VehicleType.Arrv)
                        myMap[mv.mapX, mv.mapY].cntMyArrv++;
                    if (mv.v.Type == VehicleType.Fighter)
                        myMap[mv.mapX, mv.mapY].cntMyFigher++;
                    if (mv.v.Type == VehicleType.Helicopter)
                        myMap[mv.mapX, mv.mapY].cntMyHel++;
                    if (mv.v.Type == VehicleType.Ifv)
                        myMap[mv.mapX, mv.mapY].cntMyIfv++;
                    if (mv.v.Type == VehicleType.Tank)
                        myMap[mv.mapX, mv.mapY].cntMyTank++;
                }
                else
                {
                    myMap[mv.mapX, mv.mapY].cntEnemyAll++;
                    if (mv.v.IsAerial)
                        myMap[mv.mapX, mv.mapY].cntEnemyArial++;
                    else
                        myMap[mv.mapX, mv.mapY].cntEnemyGround++;
                    if (mv.v.Type == VehicleType.Arrv)
                        myMap[mv.mapX, mv.mapY].cntEnemyArrv++;
                    if (mv.v.Type == VehicleType.Fighter)
                        myMap[mv.mapX, mv.mapY].cntEnemyFighter++;
                    if (mv.v.Type == VehicleType.Helicopter)
                        myMap[mv.mapX, mv.mapY].cntEnemyHel++;
                    if (mv.v.Type == VehicleType.Ifv)
                        myMap[mv.mapX, mv.mapY].cntEnemyIvf++;
                    if (mv.v.Type == VehicleType.Tank)
                        myMap[mv.mapX, mv.mapY].cntEnemyTank++;
                }
            }


            foreach (myFormation f in formation)
            {
                f.vencicleFormation = new List<MyVencicle>();
            }
            foreach (MyVencicle mv in allObject.Where(item => item.isMe && item.groupNum > 0))
            {
                formation[mv.groupNum - 1].vencicleFormation.Add(mv);
            }
            foreach (myFormation f in formation)
                f.update();




            foreach (myFactilies fac in myFacList)
                fac.update();


        }


    }
}