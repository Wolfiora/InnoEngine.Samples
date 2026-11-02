using System.Collections.Generic;
using Inno.VisualNovel.Compiler;
using Inno.VisualNovel.Model.Story;
using Inno.VisualNovel.Model.Values;

namespace Story.Tools.Authoring;

internal static class StorySeed
{
    internal static StoryAsset[] Create(
        StoryNodeDataCodec codec,
        List<string> voices
    ) => [First(codec, voices), Second(codec, voices), Third(codec, voices)];

    private static StoryAsset First(
        StoryNodeDataCodec codec,
        List<string> voices
    ) {
        var b = new ChapterBuilder(codec, "nz.ch01", "第一章 · 雨夜末班车", voices);
        b.Stage("StreetRain"); b.Music("city"); b.Clip("RainOpening");
        b.Lines("""
        n|雨把霓虹揉进路面。林澈拎着工具包，冲进即将关闭的地铁站。
        lin|末班车等我三分钟。我只是来修个时钟，不是来加夜班的。
        n|检修屏显示二十三点五十九分。她换下电池，秒针却纹丝不动。
        lin/surprised|新电池也罢工？这座城市总不能集体赖账吧。
        n|头顶的灯一盏接一盏熄灭。远处传来列车刹车声，随后连雨声也消失了。
        lin|喂，站务室？有人听见吗？
        """);
        b.Stage("MetroPlatform");
        b.Lines("""
        n|黑暗中，轨道间浮起一根青色的线，像某种仍在跳动的血管。
        lin/surprised|停电以后，电线还会发光？师父没教过这个。
        n|她蹲下伸手。玻璃门上的倒影却比她慢了半拍。
        lin|别告诉我，这也是设备老化。
        n|倒影张开嘴，化成一团漆黑的潮水扑来。
        xu/determined|退后！别看它的眼睛！
        """);
        b.Clip("LampReveal"); b.Stage("RainLamp"); b.Unlock("cg.rain");
        b.Lines("""
        n|银发女子挡在她身前。手心的灯印亮起，把黑影钉在雨幕里。
        lin/surprised|你刚才，空手画了一盏灯？
        xu|许岚。守灯人。现在不是解释的时候。
        lin|林澈。维修学徒。通常我会先问报价，不过今晚可以省略。
        xu|你看得见地上的光线？
        lin|还看得见它快断了。左边那根，像烧坏的保险丝。
        xu/surprised|普通人不该看见回路。跟紧我。
        """);
        b.Stage("MetroPlatform"); b.Music("conflict"); b.Actor("lin"); b.Actor("xu", "determined", "battle");
        b.Lines("""
        n|站台另一端，几个乘客被黑水困在长椅旁。闸门正在缓缓合拢。
        xu|这是夜潮。它借着停下的时间，从城市的缝隙里出来。
        lin/angry|那就把缝补上。你挡住它，我去做点会的事。
        xu|两个方向，我只能护住一个。你决定先走哪边。
        """);
        b.Choice("station-choice", "黑影涌上站台。先做什么？", "先护送乘客离开", "切断附着的线路");
        b.Branch("station-choice", "a", () => {
            b.Trust(); b.Assign("stationChoice", NovelValue.String("rescue"));
            b.Lines("""
            lin/determined|人先走！你们沿着亮线跑，别回头！
            xu|把手给我。我的护盾可以借你一半。
            lin|还能外借？好，坏了算我的！
            n|两人的掌心碰在一起。灯印分成两束，护住了最后一位乘客。
            xu/smile|没有坏。你接得很好。
            """); b.Clip("PlatformClash");
        }); string a = b.tail;
        b.Branch("station-choice", "b", () => {
            b.Assign("stationChoice", NovelValue.String("cut"));
            b.Lines("""
            lin/determined|黑水顺着旧电缆过来。给我五秒！
            xu|普通工具伤不到夜潮。
            lin|但可以让它无处下脚。
            n|绝缘钳合拢。青色火花炸开，黑影失去支点，跌回轨道。
            xu/surprised|你把载体切断了……这确实也算修理。
            """); b.Clip("PlatformClash");
        }); b.Merge(a, b.tail);
        b.Lines("""
        n|最后一扇闸门落下。乘客们消失在出口，站台终于安静。
        lin|我现在可以问一句，这是加班还是世界末日？
        xu/sad|如果钟塔敲出第十二声，两者就没有区别了。
        lin/surprised|整座城市都停在五十九分，是有人把时钟锁住了？
        xu|沈砚。他把电网改成了术式，要让这一分钟永远持续。
        lin|永远？那明天的早饭怎么办？
        xu|你应该离开。我一个人去钟塔。
        lin/determined|刚才那些人不是你一个人送出去的。
        xu|这不是维修委托。封印会收走一些东西。
        lin|先把灯修亮，再谈工钱。我熟路，也会爬检修梯。
        n|许岚望着她，终于把一枚小小的灯印递了过来。
        xu/smile|握紧。它亮着的时候，你就能找到我。
        """);
        b.Stage("LampAlley"); b.Unlock("character.lin"); b.Unlock("character.xu"); b.Unlock("glossary.tide");
        b.Lines("""
        n|小巷尽头，钟塔的影子刺穿云层。天空中的红色光环开始收紧。
        lin|等天亮，你得把整件事从头讲给我听。
        xu|如果天会亮的话。
        lin/smile|会的。维修工上门，总得解决一点问题。
        """);
        return b.Finish("nz.ch02");
    }

    private static StoryAsset Second(
        StoryNodeDataCodec codec,
        List<string> voices
    ) {
        var b = new ChapterBuilder(codec, "nz.ch02", "第二章 · 断裂回路", voices);
        b.Stage("LampWorkshop"); b.Music("hidden");
        b.Lines("""
        n|钟塔脚下的修灯铺还亮着。许岚关上门，把风雨和追来的黑影挡在外面。
        lin|你住在这里？一屋子灯，电费一定很壮观。
        xu/smile|它们吃的是故事。有人记得一件温暖的事，就会亮一点。
        lin|那我的工具包应该能照亮半条街。里面全是师父骂我的回忆。
        xu|那大概是很关心你的人。
        n|她笑了一下，随即按住胸口。指缝间的光，正在一点点变暗。
        lin/sad|你的灯印在裂。刚才为了护住我？
        xu|历代守灯人都用自己的记忆维持封印。用得越多，留下的越少。
        lin/surprised|连自己的名字也会忘？
        xu|名字可以写在纸上。更难的是，忘了为什么要记住它。
        n|墙上贴着一张旧照片。照片里的女孩站在修灯铺门口，笑得毫无防备。
        lin|这是你？
        xu/sad|也许。照片背后写着我的名字。我已经不记得拍照的人了。
        lin|那今晚不能再按老办法来。
        xu|封印需要有人承担，规矩一直如此。
        lin/determined|电路能并联，重东西能两个人抬。规矩也可以修。
        """);
        b.Unlock("glossary.lamp"); b.Unlock("glossary.keeper");
        b.Choice("workshop-choice", "许岚重新点亮灯印。你如何准备？", "让我分担术式", "我去调查另一条回路");
        b.Branch("workshop-choice", "a", () => {
            b.Trust(); b.Assign("workshopChoice", NovelValue.String("share")); b.Clip("ResonancePractice");
            b.Lines("""
            lin/determined|教我接住它。要是疼，我会喊，不会逞强。
            xu|跟着我的呼吸。不要抓住光，给它一条可以走的路。
            n|灯印在她们掌间展开。林澈看见车站、灯铺，还有素未谋面的笑脸。
            lin/surprised|这些都是你保管的记忆？好重。
            xu|现在松手还来得及。
            lin/smile|别急着赶我走。我还没接稳呢。
            n|第二束光亮了起来。许岚第一次放松了紧绷的肩膀。
            """);
        }); string a = b.tail;
        b.Branch("workshop-choice", "b", () => {
            b.Assign("workshopChoice", NovelValue.String("study"));
            b.Lines("""
            lin|你准备封印，我看看沈砚留下的图纸。总有旁路。
            n|林澈翻开旧检修册。红笔标出的线路，全都指向钟塔地下。
            lin/surprised|这里不是时间机器，是一台把记忆重复播放的设备。
            xu|沈砚以前也是守灯人。他在一次夜潮事故中失去了家人。
            lin/sad|所以他想要的，不是永远的夜晚。
            xu|是一个没有来得及说再见的早晨。
            """);
        }); b.Merge(a, b.tail);
        b.Lines("""
        n|柜台上的怀表忽然自己打开。一段旧留言从裂开的机芯中传出。
        shen|如果有人找到这份记录，请记住：零时回路只能保留影像，不能归还生命。
        lin|他明明知道。
        xu/sad|知道一件事，和接受它，是两回事。
        n|窗外的灯齐齐熄灭。只剩钟塔顶端，一圈红光像睁开的眼睛。
        lin/determined|走吧。再晚一点，他连后悔的机会也没有了。
        """);
        b.Stage("ClockTower"); b.Music("conflict"); b.Clip("CameraPush");
        b.Lines("""
        n|检修梯通向塔顶。沈砚站在巨大的钟面前，手里握着那只破损的怀表。
        shen|许岚。你还是来了。还带着一个什么都不懂的孩子。
        lin/angry|我懂一件事。你这台设备没有安全开关。
        shen|等仪式完成，城市会回到事故发生以前。所有人都能重新开始。
        xu/determined|你只能重播那一分钟。被困住的人不会有明天。
        shen/angry|明天有什么好？一张事故通知，一盏再也修不亮的灯？
        lin/sad|那盏灯，你真的一点都修不亮了吗？
        shen|我试过所有办法。只有这里，还能听见她叫我的名字。
        n|红色回路中浮起一道人影。没有脸，也没有声音，却向沈砚伸出了手。
        xu/determined|那不是她。是夜潮在模仿你的愿望！
        shen/angry|闭嘴！你把记忆交给封印，当然可以轻易忘记！
        xu/sad|我不是想忘。正因为记得太珍贵，我才不想让别人失去。
        """);
        b.Clip("TowerBreak");
        b.Lines("""
        n|怀表落进阵心。钟面反向旋转，黑色浪潮撕开了天空。
        shen/surprised|不对……我明明把负载算好了！
        lin/determined|你的愿望没有上限，负载当然也没有！许岚，左边！
        xu/determined|灯印，展开！
        n|青色护盾挡住第一道冲击。第二道却穿过裂缝，把许岚击倒在地。
        lin/surprised|许岚！看着我，别闭眼！
        xu/sad|核心在钟塔下面……如果我忘了怎么走，你带我过去。
        lin/determined|我会。但你也得记住回来。我们说好了，要等天亮。
        """);
        b.Unlock("character.shen"); b.Unlock("glossary.circuit"); b.Unlock("replay.tower");
        return b.Finish("nz.ch03");
    }

    private static StoryAsset Third(
        StoryNodeDataCodec codec,
        List<string> voices
    ) {
        var b = new ChapterBuilder(codec, "nz.ch03", "第三章 · 最后一盏灯", voices);
        b.Stage("ZeroCore"); b.Music("battle"); b.Clip("FinalDefense");
        b.Lines("""
        n|钟塔地下没有机器，只有一片悬在黑暗中的城市倒影。每一扇窗，都是一段记忆。
        lin/surprised|原来整座城，一直点着这么多灯。
        xu|别让夜潮碰到它们。灯灭了，人们就会忘记自己要回哪里。
        shen/sad|是我打开了门。我来关。
        lin|你终于肯修自己的烂摊子了。
        shen|骂我可以，等出去以后。核心已经超过承载极限。
        n|黑潮化成巨大的手，抓向悬在半空的灯火。许岚拖着伤臂，重新举起灯印。
        xu/determined|林澈，沿青线走。我会挡住它。
        lin/angry|你总把最重的那一头留给自己！
        xu|因为我知道怎么承担。
        lin/determined|那现在，也看看别人会什么。
        """);
        b.Actor("lin", "determined", "battle"); b.Actor("xu", "determined", "battle"); b.Clip("FinalCounter");
        b.Lines("""
        n|林澈把绝缘钳插进回路。青光沿工具包上的旧铜线奔涌，照亮了她的脸。
        lin|我看见旁路了！不是一根，是整座城市！
        shen/surprised|公共照明网……每盏灯都是一个节点。
        lin|你用它们困住过去，我们就用它们把现在接回来。
        xu/sad|共鸣不够的话，记忆会在分流时散掉。你可能再也认不出我们。
        lin|你也一样。所以你才一直不肯教别人，对吗？
        xu|我不想再看见有人替我付出代价。
        lin/determined|一起承担，不是谁替谁。你可以相信我一次。
        n|沈砚把怀表从阵心取出。模仿家人的黑影随之碎裂，露出一张空白的脸。
        shen/sad|原来我一直听见的，只有自己。
        lin/sad|真的声音，还在你记得的地方。别把它也赔进去。
        shen|好。我切断回流。剩下的，交给你们。
        """);
        b.Unlock("replay.final");
        b.Choice("final-choice", "最后的灯印，只够启动一次封印。", "把灯交给整座城市", "完成守灯人的封印", true);
        b.Branch("final-choice", "a", () => {
            b.Assign("ending", NovelValue.String("city")); b.Clip("SealCollapse"); b.Stage("ResonantDawn"); b.Unlock("cg.resonance");
            b.Lines("""
            lin/determined|不是最后一盏。还有一万盏、十万盏。许岚，和我一起！
            xu/determined|灯印共鸣——把归途，还给每一个人！
            shen/determined|回流已断。现在，点亮它！
            n|青色光流越过钟塔，奔向站台、街角和每一扇窗。夜潮在万千灯火间失去形状。
            n|有人记起家里的热汤，有人记起还没说出口的道歉，有人只是想赶上明天的第一班车。
            lin/smile|听见了吗？这座城市，还想往前走。
            xu/smile|听见了。这次，不只有我一个人的声音。
            """);
            b.Stage("RooftopDawn"); b.Music("dawn"); b.Clip("DawnReveal");
            b.Lines("""
            n|钟声终于越过零点。雨停了，第一束晨光落在三个人身上。
            shen/sad|我会留下来，把坏掉的线路一根根修好。然后，去认真说一次再见。
            lin/smile|修理费很贵。你可以先从搬工具开始。
            xu/smile|你还记得，昨晚答应过什么吗？
            lin|天亮以后，从头讲给我听。最好边吃早饭边讲。
            xu|好。从很久以前，有人开了一间修灯铺说起。
            """);
            b.Stage("CityLights"); b.Unlock("cg.city"); b.Unlock("ending.city");
            b.Lines("""
            n|街上的灯逐一熄灭，把天空交还给太阳。这一次，它们只是完成了一个平常的夜晚。
            lin/smile|走吧。今天的第一张维修单，是给修灯铺换个新招牌。
            xu/smile|写什么？
            lin|灯会坏，天会亮。欢迎明天再来。
            n|结局一 · 万家灯火
            """); b.End("end-city");
        });
        b.Branch("final-choice", "b", () => {
            b.Assign("ending", NovelValue.String("morning")); b.Clip("SealCollapse");
            b.Lines("""
            lin/sad|我还没有接住全部的光。可我不能拿这座城赌一次。
            xu/smile|你已经替我找到了路。剩下这一步，让我走完。
            lin|你会忘记我吗？
            xu/sad|也许。所以你要再告诉我一遍。
            lin/determined|我叫林澈。修东西很快，吃早饭很慢。答应过陪你等天亮。
            xu/smile|林澈。好听的名字。
            n|许岚松开手。灯印化成无数细小的光，把黑潮和停滞的时间一并送回夜里。
            shen/sad|核心关闭了……她把所有记忆，都留给了城市。
            lin/sad|不是所有。至少，这一刻我会记着。
            """);
            b.Stage("StreetDawn"); b.Music("dawn"); b.Clip("DawnReveal");
            b.Lines("""
            n|一周后，地铁时钟恢复准点。沈砚留在检修队，从最旧的一段电缆开始修起。
            n|林澈每天经过修灯铺。第七天，门终于开了。
            xu|你好。请问，这盏灯是你送来修的吗？
            lin/smile|不是。我来应聘。听说这里缺一个会接线的学徒。
            xu|我好像不太记得……该怎么修了。
            lin|没关系。我教你。工钱可以用早饭抵。
            n|许岚接过工具包，指尖碰到那枚已经不再发光的灯印。她没有想起什么，却忽然笑了。
            xu/smile|我们以前，是不是一起等过一场雨？
            lin/sad|是。后来，天亮了。
            """);
            b.Stage("MorningReunion"); b.Unlock("cg.morning"); b.Unlock("ending.morning");
            b.Lines("""
            n|新招牌在晨风里轻轻摇晃。丢失的故事未必会回来，但还没有发生的故事，正等着被写下。
            xu/smile|那今天，也一起吃早饭吧。
            lin/smile|好。这次我请。
            n|结局二 · 黎明仍至
            """);
        });
        return b.Finish();
    }
}
