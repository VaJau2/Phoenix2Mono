#!/usr/bin/env perl
# Этап 2 миграции: механические переименования C# API Godot 3 -> Godot 4.
#
# В отличие от встроенного конвертера Godot:
#   - строки, символьные литералы и комментарии не трогаются (там пути к узлам, тексты, имена ресурсов);
#   - классы Godot переименовываются только в позиции типа (объявление, generic-аргумент, приведение,
#     is/as/new/typeof, базовый класс) — поэтому свойство `player.Camera` или `Texture = icon`
#     остаются как есть, а `private Camera cam` становится `Camera3D cam`.
# Остальное (Variant, File/Directory, MoveAndSlide, ...) правится вручную по ошибкам компилятора.
#
# Запуск из корня репозитория: perl docs/migration/tools/cs_renames.pl [файлы...]
# Без аргументов обрабатываются все .cs в assets/scripts.

use strict;
use warnings;

my %types = (
    Spatial => 'Node3D', KinematicBody => 'CharacterBody3D', RigidBody => 'RigidBody3D',
    StaticBody => 'StaticBody3D', Area => 'Area3D', CollisionShape => 'CollisionShape3D',
    CollisionObject => 'CollisionObject3D', PhysicsBody => 'PhysicsBody3D',
    RayCast => 'RayCast3D', Camera => 'Camera3D', MeshInstance => 'MeshInstance3D',
    Skeleton => 'Skeleton3D', BoneAttachment => 'BoneAttachment3D', PhysicalBone => 'PhysicalBone3D',
    Position3D => 'Marker3D', Particles => 'GpuParticles3D', CPUParticles => 'CpuParticles3D',
    Light => 'Light3D', OmniLight => 'OmniLight3D', SpotLight => 'SpotLight3D',
    DirectionalLight => 'DirectionalLight3D', Path => 'Path3D', PathFollow => 'PathFollow3D',
    VisibilityNotifier => 'VisibleOnScreenNotifier3D', Listener => 'AudioListener3D',
    SpatialMaterial => 'StandardMaterial3D', Transform => 'Transform3D', Quat => 'Quaternion',
    Texture => 'Texture2D', StreamTexture => 'CompressedTexture2D', AudioStreamSample => 'AudioStreamWav',
    Reference => 'RefCounted', ParticlesMaterial => 'ParticleProcessMaterial',
    NavigationMeshInstance => 'NavigationRegion3D', DynamicFont => 'FontFile',
    VisualInstance => 'VisualInstance3D', GeometryInstance => 'GeometryInstance3D',
);
my $type_re = join '|', sort { length($b) <=> length($a) } keys %types;

# Простые переименования идентификаторов (только в коде): шаблон => замена
my @renames = (
    [qr/\bGodot\.Object\b/, 'Godot.GodotObject'],
    [qr/(?<![\w.])Object\.IsInstanceValid\b/, 'GodotObject.IsInstanceValid'],
    [qr/\.Instance(?=\s*(?:<|\())/, '.Instantiate'],
    [qr/\bGlobalTranslation\b/, 'GlobalPosition'],
    [qr/\bTranslation\b/, 'Position'],
    [qr/\.(x|y|z|w|r|g|b|a)\b(?!\s*\()/, sub { '.' . uc $1 }],
    [qr/\.origin\b/, '.Origin'],
    [qr/\.basis\b/, '.Basis'],
    [qr/\bMathf\.Deg2Rad\b/, 'Mathf.DegToRad'],
    [qr/\bMathf\.Rad2Deg\b/, 'Mathf.RadToDeg'],
    [qr/\bMathf\.Stepify\b/, 'Mathf.Snapped'],
    [qr/\.LinearInterpolate\(/, '.Lerp('],
    [qr/\bRectGlobalPosition\b/, 'GlobalPosition'],
    [qr/\bRectPosition\b/, 'Position'],
    [qr/\bRectSize\b/, 'Size'],
    [qr/\bRectMinSize\b/, 'CustomMinimumSize'],
    [qr/\bRectScale\b/, 'Scale'],
    [qr/\bRectRotation\b/, 'RotationDegrees'],
    [qr/\bRectPivotOffset\b/, 'PivotOffset'],
    [qr/\bUnitDb\b/, 'VolumeDb'],
    [qr/\bBbcodeText\b/, 'Text'],
    [qr/\bCastTo\b/, 'TargetPosition'],
    [qr/\bSetShaderParam\b/, 'SetShaderParameter'],
    [qr/\bGetShaderParam\b/, 'GetShaderParameter'],
    [qr/\bSetSurfaceMaterial\b/, 'SetSurfaceOverrideMaterial'],
    [qr/\bGetSurfaceMaterial\b/, 'GetSurfaceOverrideMaterial'],
    [qr/\bGetScancodeString\b/, 'GetKeycodeString'],
    [qr/\bPhysicalScancode\b/, 'PhysicalKeycode'],
    [qr/\bScancode\b/, 'Keycode'],
    [qr/\bInputMap\.GetActionList\b/, 'InputMap.ActionGetEvents'],
    [qr/\.Filename\b/, '.SceneFilePath'],
    [qr/\bFindNode\(/, 'FindChild('],
    [qr/\bGetWorld\(\)/, 'GetWorld3D()'],
    [qr/\bNavigationServer\./, 'NavigationServer3D.'],
    [qr/\bVisualServer\b/, 'RenderingServer'],
    [qr/\bPhysicsServer\b/, 'PhysicsServer3D'],
    [qr/\bPauseModeEnum\.Process\b/, 'ProcessModeEnum.Always'],
    [qr/\bPauseModeEnum\.Stop\b/, 'ProcessModeEnum.Pausable'],
    [qr/\bPauseModeEnum\.Inherit\b/, 'ProcessModeEnum.Inherit'],
    [qr/\bPauseModeEnum\b/, 'ProcessModeEnum'],
    [qr/\bPauseMode\b/, 'ProcessMode'],
    [qr/\bOS\.GetTicksMsec\b/, 'Time.GetTicksMsec'],
    [qr/\bOS\.GetTicksUsec\b/, 'Time.GetTicksUsec'],
    [qr/\bButtonList\./, 'MouseButton.'],
    [qr/\bKeyList\./, 'Key.'],
    [qr/\bPercentVisible\b/, 'VisibleRatio'],
    [qr/\bUnitOffset\b/, 'ProgressRatio'],
    [qr/\bFogColor\b/, 'FogLightColor'],
    [qr/\bFilterDB\.Filter(\d+)db\b/, sub { "FilterDB.Filter${1}Db" }],
    [qr/\bFilterDb\.Filter(\d+)Db\b/, sub { "FilterDB.Filter${1}Db" }],
    [qr/\bGetCamera\(\)/, 'GetCamera3D()'],
    [qr/\bCaretPosition\b/, 'CaretColumn'],
    [qr/\bAdd(Color|Font|Stylebox|Constant|Icon)Override\(/, sub { "AddTheme${1}Override(" }],
    [qr/\.Quat\(\)/, '.GetRotationQuaternion()'],
    [qr/\bPlaybackSpeed\b/, 'SpeedScale'],
    [qr/\.BeginsWith\(/, '.StartsWith('],
    [qr/\.Empty\(\)/, '.IsEmpty()'],
    [qr/\b(override\s+void\s+_(?:Physics)?Process\s*\(\s*)float\b/, '$1double'],
);

my @files = @ARGV;
if (!@files) {
    open(my $git, '-|', 'git', 'ls-files', '-z', 'assets/scripts/*.cs') or die $!;
    local $/ = "\0";
    while (my $f = <$git>) { chomp $f; push @files, $f; }
    close $git;
}

# Сигналы: собираем имена всех [Signal] delegate по проекту
my %signals;
for my $file (@files) {
    my $text = read_file($file);
    $signals{$1} = 1 while $text =~ /\[Signal\]\s*(?:public\s+)?delegate\s+\w+\s+(\w+?)(?:EventHandler)?\s*\(/g;
}
my $signal_re = join '|', map { quotemeta } sort { length($b) <=> length($a) } keys %signals;

my $changed = 0;
for my $file (@files) {
    my $orig = read_file($file);
    my $text = $orig;

    # Шаблоны, которые захватывают строки, — до разбиения на сегменты
    $text =~ s/ToSignal\(\s*((?:[\w.]+\.)?GetTree\(\))\s*,\s*"idle_frame"\s*\)/ToSignal($1, SceneTree.SignalName.ProcessFrame)/g;
    $text =~ s/ToSignal\(\s*((?:[\w.]+\.)?GetTree\(\))\s*,\s*"physics_frame"\s*\)/ToSignal($1, SceneTree.SignalName.PhysicsFrame)/g;

    # obj.Connect(signal, target, "method" | nameof(method)[, ...]) -> obj.Connect(signal, new Callable(target, method)[, ...])
    # (сигнал и метод могут быть строками, поэтому — до разбиения на сегменты)
    $text =~ s/(?<!BoundCall)\.(Connect|Disconnect|IsConnected)\(\s*("[^"\n]*"|nameof\([^()]*\)|[\w.]+(?:\([^()]*\))?)\s*,\s*([\w.?]+)\s*,\s*("[^"\n]*"|nameof\(\s*[\w.]+\s*\)|\w+)\s*(?=[,)])/.$1($2, new Callable($3, $4)/g;

    $text = transform_code($text, \&rename_code);

    if ($text ne $orig) {
        write_file($file, $text);
        $changed++;
    }
}
print "DONE files=$changed signals=", scalar(keys %signals), "\n";

sub rename_code {
    my ($code) = @_;

    # partial для всех классов с базовым типом
    $code =~ s/\b((?:(?:public|internal|private|protected|abstract|sealed|static|partial)\s+)*)class(\s+\w+\s*(?:\(|:|<))/
        my ($mods, $rest) = ($1, $2);
        $mods =~ m{\bpartial\b} ? "${mods}class$rest" : "${mods}partial class$rest"/ge;

    # [Signal] delegate void X( -> XEventHandler(
    $code =~ s/(\[Signal\]\s*(?:public\s+)?delegate\s+\w+\s+)(\w+?)(?<!EventHandler)(\s*\()/$1$2EventHandler$3/g;

    # nameof(Signal) / nameof(Class.Signal) -> SignalName.Signal / Class.SignalName.Signal
    if ($signal_re) {
        $code =~ s/\bnameof\(\s*(?:(\w+)\.)?($signal_re)\s*\)/defined $1 ? "$1.SignalName.$2" : "SignalName.$2"/ge;
    }

    # obj.Connect(signal, target, method) -> obj.Connect(signal, new Callable(target, method))
    $code =~ s/\.(Connect|Disconnect|IsConnected)\(\s*([^,()]+(?:\([^()]*\))?[^,()]*)\s*,\s*([\w.?]+)\s*,\s*(nameof\(\s*[\w.]+\s*\)|[\w.]+)\s*\)/.$1($2, new Callable($3, $4))/g;

    # Классы Godot в позиции типа
    my $t = $type_re;
    $code =~ s/(?<![\w.])($t)(?=\s*(?:\[\])?\??\s+[A-Za-z_]\w*\s*(?:[=;,)(]|\{|=>|\bin\b))/$types{$1}/g;   # объявление: Type name
    $code =~ s/(?<=<)\s*($t)(?=\s*[>,])/$types{$1}/g;                                     # GetNode<Type>, List<Type, ...>
    $code =~ s/(?<=,)(\s*)($t)(?=\s*>)/$1$types{$2}/g;                                     # Dictionary<string, Type>
    $code =~ s/(?<=\()\s*($t)(?=\s*\)\s*[\w(])/$types{$1}/g;                              # (Type)expr
    $code =~ s/\b(is|as|new|typeof\(|is not)(\s+|\()($t)\b(?!\.)/$1$2$types{$3}/g;         # is/as/new/typeof
    $code =~ s/\btypeof\(\s*($t)\s*\)/typeof($types{$1})/g;
    $code =~ s/(\bclass\s+\w+\s*(?:<[^>]*>)?\s*:\s*)($t)\b/$1$types{$2}/g;                 # базовый класс
    $code =~ s/(\bwhere\s+\w+\s*:\s*)($t)\b/$1$types{$2}/g;
    $code =~ s/(?<![\w.])($t)(?=\.\w+Enum\b)/$types{$1}/g;                                 # PathFollow.RotationModeEnum

    for my $r (@renames) {
        my ($re, $to) = @$r;
        if (ref $to eq 'CODE') { $code =~ s/$re/$to->()/ge }
        else { $code =~ s/$re/qq{"$to"}/gee }
    }
    return $code;
}

# Применяет $fn только к участкам кода, пропуская строки, символы и комментарии
sub transform_code {
    my ($text, $fn) = @_;
    my $out = '';
    my $code = '';
    pos($text) = 0;
    while (pos($text) < length $text) {
        if ($text =~ /\G(\/\/[^\n]*|\/\*.*?\*\/)/gcs                         # комментарии
            || $text =~ /\G(\$@"(?:[^"]|"")*"|@\$"(?:[^"]|"")*"|@"(?:[^"]|"")*")/gc   # verbatim-строки
            || $text =~ /\G(\$?"(?:[^"\\\n]|\\.)*")/gc                         # строки и $"..."
            || $text =~ /\G('(?:[^'\\\n]|\\.)+')/gc) {                          # символы
            $out .= $fn->($code) . $1;
            $code = '';
        }
        elsif ($text =~ /\G([^\/"'\$@]+|.)/gcs) {
            $code .= $1;
        }
    }
    return $out . $fn->($code);
}

sub read_file {
    my ($file) = @_;
    open(my $fh, '<:raw', $file) or die "$file: $!";
    local $/;
    my $text = <$fh>;
    close $fh;
    return $text;
}

sub write_file {
    my ($file, $text) = @_;
    open(my $fh, '>:raw', $file) or die "$file: $!";
    print $fh $text;
    close $fh;
}
